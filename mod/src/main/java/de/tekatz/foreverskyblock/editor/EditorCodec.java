package de.tekatz.foreverskyblock.editor;

import com.google.gson.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import de.tekatz.foreverskyblock.progression.*;
import static de.tekatz.foreverskyblock.editor.EditorDocument.*;

/** Pure bounded validation used by imports, networking, storage, undo and publication. */
public final class EditorCodec {
    public static final Gson JSON = new GsonBuilder().setPrettyPrinting().disableHtmlEscaping().create();
    public static final Gson WIRE = new GsonBuilder().disableHtmlEscaping().create();
    private EditorCodec() { }
    public static EditorDocument decode(String source) {
        require(source != null && source.getBytes(StandardCharsets.UTF_8).length<=MAX_BYTES,"Document exceeds 240 KB");
        // Bound nesting before Gson constructs the object graph.
        int depth=0; boolean quoted=false,escape=false;
        for(char c:source.toCharArray()) {
            if(escape){escape=false;continue;} if(quoted&&c=='\\'){escape=true;continue;}
            if(c=='"'){quoted=!quoted;continue;} if(!quoted){if(c=='{'||c=='[')require(++depth<=24,"JSON nesting too deep");else if(c=='}'||c==']')depth--;}
        }
        try { var result=JSON.fromJson(source,EditorDocument.class); validate(result); return result; }
        catch(RuntimeException e) { throw new IllegalArgumentException("Invalid K-menu document: "+e.getMessage(),e); }
    }
    public static String encode(EditorDocument document) { validate(document); String value=JSON.toJson(document); require(value.getBytes(StandardCharsets.UTF_8).length<=MAX_BYTES,"Document exceeds 240 KB");return value; }
    public static void validate(EditorDocument d) {
        require(d!=null&&d.schema()==SCHEMA&&d.revision()>=0,"Unsupported schema or revision");
        require(d.quests().size()<=128&&d.chapters().size()<=32&&d.products().size()<=32&&d.contracts().size()<=32
                &&d.layout().size()<=256&&d.nodes().size()<=256&&d.decorations().size()<=64,"Too many entries");
        var chapters=new HashSet<String>(); for(var c:d.chapters()){key(c.id());unique(chapters,c.id());text(c.title(),120);id(c.icon());range(c.order(),-1000,1000);}
        var ids=new HashSet<String>();
        for(var q:d.quests()) {
            id(q.id());unique(ids,q.id());text(q.title(),160);text(q.description(),6000);id(q.icon());
            require(chapters.contains(q.chapter()),"Missing chapter: "+q.chapter());
            require(q.taskMatch()!=null&&q.prerequisiteMatch()!=null&&!q.tasks().isEmpty()&&q.tasks().size()<=16&&q.requires().size()<=32,"Invalid task structure");
            range(q.requiredTier(),-1,4);range(q.rewardSp(),0,10000);range(q.rewardPp(),0,1000);
            require(q.rewardSp()+q.rewardPp()>0,"A quest needs a reward");
            var taskIds=new HashSet<String>();
            for(var t:q.tasks()) {
                key(t.id());unique(taskIds,t.id());require(t.type()!=null,"Missing task type");id(t.target());range(t.count(),1,1_000_000);
                if(t.type()==TaskType.LEGACY) {
                    var original=ChallengeRules.find(q.id());
                    require(original!=null&&q.legacy()&&t.target().equals(q.id()),"Legacy tasks must retain their original ID");
                    require(t.count()<=original.target(),"Legacy objective cannot exceed its original evidence capacity");
                }
            }
            if(ChallengeRules.find(q.id())!=null)require(q.legacy(),"Built-in challenges must retain their original trigger");
            else require(q.id().startsWith("forever_skyblock:quest/"),"Custom quest IDs must start with forever_skyblock:quest/");
        }
        for(var q:d.quests())for(String dependency:q.requires())require(ids.contains(dependency)&&!dependency.equals(q.id()),"Missing or self prerequisite: "+dependency);
        var visited=new HashSet<String>();var visiting=new HashSet<String>();for(var q:d.quests())cycle(d,q.id(),visited,visiting);
        var products=new HashSet<String>();
        for(var p:d.products()) {
            id(p.id());unique(products,p.id());text(p.title(),160);text(p.description(),6000);id(p.icon());range(p.requiredTier(),0,4);range(p.spCost(),1,1_000_000);range(p.ppCost(),0,1_000_000);
            require(p.contents().size()<=16&&p.requires().size()<=32,"Invalid product");for(String dep:p.requires())require(ids.contains(dep),"Missing product prerequisite");
            for(var stack:p.contents()){id(stack.item());range(stack.count(),1,64);}
            var original=ShopCatalog.PRODUCTS.stream().filter(x->x.id().equals(p.id())).findFirst().orElse(null);
            require(original!=null||p.id().startsWith("forever_skyblock:shop/custom/"),"Invalid custom product ID");
            require(original!=null||!p.contents().isEmpty(),"New products need package contents");
            if(original!=null)require(original.contents().equals(p.contents().stream().map(s->new ShopCatalog.DeliveryEntry(s.item(),s.count())).toList()),"Existing package contents are immutable; create a new product ID");
        }
        var contracts=new HashSet<String>();
        for(var c:d.contracts()) {
            id(c.id());unique(contracts,c.id());text(c.title(),160);text(c.description(),6000);id(c.icon());require(c.type()!=null,"Missing contract type");
            range(c.requiredTier(),0,4);range(c.count(),1,1_000_000);range(c.rewardSp(),1,10);if(c.type()==ContractType.RESOURCE)id(c.item());
            require(ContractCatalog.CONTRACTS.stream().anyMatch(x->x.id().equals(c.id()))||c.id().startsWith("forever_skyblock:contract/custom/"),"Invalid contract ID");
            require(!ids.contains(c.id())&&!products.contains(c.id()),"ID collision");
        }
        d.layout().forEach((key,s)->{require(key.length()<=160&&key.matches("[a-zA-Z0-9_./:-]+"),"Invalid layout key");range(s.x(),-4096,4096);range(s.y(),-4096,4096);range(s.width(),0,4096);range(s.height(),0,4096);
            require(s.anchorX()!=null&&s.anchorY()!=null,"Missing anchor");finite(s.fontScale(),.25F,4);finite(s.itemScale(),.25F,4);color(s.fill());color(s.border());color(s.text());});
        d.nodes().forEach((key,n)->{require(ids.contains(key)||products.contains(key)||key.startsWith("shop:"),"Unknown graph node: "+key);finite(n.x(),-4096,4096);finite(n.y(),-4096,4096);range(n.radius(),10,60);});
        var decorations=new HashSet<String>();for(var w:d.decorations()) {
            key(w.id());unique(decorations,w.id());require(Set.of("ALL","OVERVIEW","CHALLENGES","SHOP","RESEARCH","PERKS","PROFILE").contains(w.page())&&w.type()!=null,"Invalid decoration page/type");
            text(w.title(),300);id(w.icon());require(Set.of("","SP","PP","TIER","PLAYTIME","ISLAND","DEATHS","KILLS","CHALLENGES").contains(w.binding()),"Unknown binding");
            range(w.x(),-4096,4096);range(w.y(),-4096,4096);range(w.width(),1,4096);range(w.height(),1,4096);color(w.color());
        }
        range(d.settings().grid(),1,64);range(d.settings().animationMillis(),0,2000);finite(d.settings().scrollSpeed(),1,200);
    }
    /** Published entries are tombstoned/disabled rather than removed, preserving old progress and entitlements. */
    public static void validateUpdate(EditorDocument old,EditorDocument next) {
        validate(next);
        for(var q:old.quests()) {
            var n=next.quest(q.id());require(n!=null,"Disable published quests instead of deleting: "+q.id());
            require(q.tasks().size()==n.tasks().size(),"Published task identities are immutable; duplicate the quest for a new structure");
            for(int i=0;i<q.tasks().size();i++){var a=q.tasks().get(i);var b=n.tasks().get(i);require(a.id().equals(b.id())&&a.type()==b.type()&&a.target().equals(b.target()),"Published task identity changed");}
        }
        for(var p:old.products()){var n=next.product(p.id());require(n!=null&&n.contents().equals(p.contents()),"Published package contents/IDs are immutable; disable or duplicate");}
        for(var c:old.contracts()){var n=next.contract(c.id());require(n!=null&&n.type()==c.type()&&Objects.equals(n.item(),c.item()),"Published contract type/item is immutable");}
    }
    private static void cycle(EditorDocument d,String id,Set<String> done,Set<String> stack){if(done.contains(id))return;require(stack.add(id),"Quest prerequisite cycle");for(String dep:d.quest(id).requires())cycle(d,dep,done,stack);stack.remove(id);done.add(id);}
    public static int color(String value,int fallback){return value==null||value.isEmpty()?fallback:(int)Long.parseLong(value.replace("#",""),16);}
    private static void color(String s){require(s!=null&&(s.isEmpty()||s.matches("#?[0-9a-fA-F]{8}")),"Colors use AARRGGBB");}
    private static void key(String s){require(s!=null&&s.matches("[a-z0-9_./-]{1,64}"),"Invalid local ID");}
    private static void text(Text s,int max){require(s!=null&&s.en()!=null&&!s.en().isBlank()&&s.en().length()<=max&&s.de()!=null&&s.de().length()<=max,"Invalid localized text");}
    private static void id(String id){ProgressionProfile.validateId(id);}
    private static void unique(Set<String> set,String id){require(set.add(id),"Duplicate ID: "+id);}
    private static void finite(float v,float low,float high){require(Float.isFinite(v)&&v>=low&&v<=high,"Numeric value out of range");}
    private static void range(long v,long low,long high){require(v>=low&&v<=high,"Numeric value out of range");}
    public static void require(boolean ok,String message){if(!ok)throw new IllegalArgumentException(message);}
}
