package de.tekatz.foreverskyblock.editor;

import de.tekatz.foreverskyblock.progression.*;
import java.util.*;
import static de.tekatz.foreverskyblock.editor.EditorDocument.*;

/** Published catalog only. Client drafts are deliberately held elsewhere. */
public final class EditorRuntime {
    private static volatile EditorDocument active;
    private EditorRuntime() { }
    public static EditorDocument active(){return active;}
    public static void publish(EditorDocument document){active=document;}
    public static Quest quest(String id){var d=active;return d==null?null:d.quest(id);}
    public static boolean productUnlocked(String id,ProgressionProfile profile){var d=active;var p=d==null?null:d.product(id);return p==null||p.enabled()&&QuestRules.dependencies(p.requires(),Match.ALL,profile.milestones());}
    public static List<ShopCatalog.Product> products() {
        var d=active;if(d==null)return ShopCatalog.PRODUCTS;
        var result=new ArrayList<ShopCatalog.Product>();
        for(var p:d.products()) {
            var original=ShopCatalog.PRODUCTS.stream().filter(o->o.id().equals(p.id())).findFirst().orElse(null);
            // Disabled products stay resolvable to finish earlier paid transactions.
            var kind=original==null?ShopCatalog.Kind.SUPPLY_PACKAGE:original.kind();
            result.add(new ShopCatalog.Product(p.id(),p.title().en(),p.title().de().isBlank()?p.title().en():p.title().de(),p.description().en(),p.description().de().isBlank()?p.description().en():p.description().de(),
                    p.requiredTier(),p.spCost(),p.ppCost(),kind,p.contents().stream().map(s->new ShopCatalog.DeliveryEntry(s.item(),s.count())).toList()));
        }
        for(var p:ShopCatalog.PRODUCTS)if(d.product(p.id())==null)result.add(p);
        return List.copyOf(result);
    }
    public static List<ContractCatalog.Contract> contracts() {
        var d=active;if(d==null)return ContractCatalog.CONTRACTS;
        var result=new ArrayList<ContractCatalog.Contract>();
        for(var c:d.contracts())result.add(new ContractCatalog.Contract(c.id(),ContractCatalog.Trigger.valueOf(c.type().name()),c.requiredTier(),c.count(),c.rewardSp(),c.type()==ContractType.RESOURCE?c.item():null,c.icon(),
                c.title().en(),c.title().de().isBlank()?c.title().en():c.title().de(),c.description().en(),c.description().de().isBlank()?c.description().en():c.description().de()));
        for(var c:ContractCatalog.CONTRACTS)if(d.contract(c.id())==null)result.add(c);
        return List.copyOf(result);
    }
    public static boolean contractEnabled(String id){var d=active;var c=d==null?null:d.contract(id);return c==null||c.enabled();}
    public static boolean customContract(String id){var d=active;return d!=null&&d.contract(id)!=null;}
}
