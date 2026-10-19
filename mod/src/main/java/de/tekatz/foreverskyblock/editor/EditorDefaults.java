package de.tekatz.foreverskyblock.editor;

import com.google.gson.*;
import java.io.*;
import java.nio.charset.StandardCharsets;
import java.util.*;
import de.tekatz.foreverskyblock.progression.*;
import static de.tekatz.foreverskyblock.editor.EditorDocument.*;

/** The initial authoring document mirrors existing rules without changing their triggers or rewards. */
public final class EditorDefaults {
    public static final List<String> ELEMENTS=List.of(
            "header.brand","header.sp","header.sp.icon","header.sp.label","header.sp.value",
            "header.pp","header.pp.icon","header.pp.label","header.pp.value",
            "header.tier","header.tier.icon","header.tier.label","header.tier.value",
            "header.stats","header.stats.playtime.icon","header.stats.playtime.label","header.stats.playtime.value",
            "header.stats.island.icon","header.stats.island.label","header.stats.island.value",
            "nav.challenges","nav.shop","nav.research","nav.perks","nav.profile","search","categories",
            "body.challenges","body.shop","body.research","body.perks","body.profile",
            "card","card.title","card.description","card.icon","card.reward","detail","detail.title","detail.icon","detail.prose","profile.identity","profile.stats");
    private EditorDefaults() { }
    public static EditorDocument create() {
        var en=language("en_us");var de=language("de_de");
        String[] cats={"forever","ae2","mekanism","powah","mystical_agriculture","combat","contracts"};
        var chapters=new ArrayList<Chapter>();for(int i=0;i<cats.length;i++)chapters.add(new Chapter(cats[i],new Text(category(value(en,"gui.forever_skyblock.meta.sub."+cats[i],cats[i])),category(value(de,"gui.forever_skyblock.meta.sub."+cats[i],cats[i]))),"",i));
        String[] icons={"cobblestone","flint","stone_pickaxe","iron_pickaxe","diamond_pickaxe","netherite_pickaxe","netherrack","nether_star","end_stone","dragon_head","iron_sword","chest","crafting_table","redstone","compass","ender_chest","iron_ingot","diamond_pickaxe","glowstone_dust","nether_star"};
        var quests=new ArrayList<Quest>();int i=0;
        for(var q:ChallengeRules.ALL) {
            String key="milestone."+q.id().replace(':','.');
            quests.add(new Quest(q.id(),true,new Text(value(en,key,q.id()),value(de,key,q.id())),
                    new Text(value(en,key+".description","Complete this challenge."),value(de,key+".description","Schließe diese Herausforderung ab.")),
                    "minecraft:"+icons[i++],q.category(),List.of(new Task("legacy",TaskType.LEGACY,q.id(),q.target())),Match.ALL,List.of(),Match.ALL,-1,q.reward(),0));
        }
        var products=new ArrayList<Product>();for(var p:ShopCatalog.PRODUCTS)products.add(new Product(p.id(),p!=ShopCatalog.TEST,new Text(p.titleEn(),p.titleDe()),
                new Text(p.descriptionEn(),p.descriptionDe()),"minecraft:"+(p==ShopCatalog.NO_PHANTOMS?"phantom_membrane":p==ShopCatalog.NO_RAIN?"water_bucket":p==ShopCatalog.PERMANENT_DAY?"sunflower":"chest"),
                p.requiredTier(),p.spCost(),p.ppCost(),List.of(),p.contents().stream().map(s->new EditorDocument.Stack(s.itemId(),s.count())).toList()));
        var contracts=new ArrayList<Contract>();for(var c:ContractCatalog.CONTRACTS)contracts.add(new Contract(c.id(),true,new Text(c.titleEn(),c.titleDe()),
                new Text(c.descriptionEn(),c.descriptionDe()),c.iconId(),ContractType.valueOf(c.trigger().name()),c.itemId()==null?"":c.itemId(),c.target(),c.rewardSp(),c.requiredTier()));
        var layout=new LinkedHashMap<String,Style>();for(String id:ELEMENTS)layout.put(id,Style.defaults());
        return new EditorDocument(SCHEMA,0,layout,chapters,quests,products,contracts,Map.of(),List.of(),new Settings(4,true,200,29,false,6,3));
    }
    private static JsonObject language(String lang) {
        try(var in=EditorDefaults.class.getResourceAsStream("/assets/forever_skyblock/lang/"+lang+".json")) {
            return in==null?new JsonObject():JsonParser.parseReader(new InputStreamReader(in,StandardCharsets.UTF_8)).getAsJsonObject();
        }catch(IOException e){throw new IllegalStateException(e);}
    }
    private static String category(String value){int at=value.indexOf(": ");return at<0?value:value.substring(at+2);}
    private static String value(JsonObject json,String key,String fallback){return json.has(key)?json.get(key).getAsString():fallback;}
}
