package de.tekatz.foreverskyblock.editor;

import de.tekatz.foreverskyblock.progression.ShopCatalog;
import java.util.*;
import static de.tekatz.foreverskyblock.editor.EditorDocument.*;

/** A shared 640-unit canvas keeps editor nodes, live painters, links and hit areas aligned. */
public final class GraphGeometry {
    public static final List<String> SHOP_ROOTS=List.of("shop:permanent","shop:tier0","shop:reach","shop:tier1");
    private GraphGeometry() { }
    public static Node point(EditorDocument d,String id,boolean shop) {
        var saved=d.nodes().get(id);if(saved!=null)return saved;
        if(shop){
            if(id.equals("shop:permanent"))return new Node(80,22,18);
            if(id.equals("shop:tier0"))return new Node(400,22,18);
            if(id.equals("shop:reach"))return new Node(80,108,18);
            if(id.equals("shop:tier1"))return new Node(400,245,18);
            if(id.equals(ShopCatalog.NO_PHANTOMS.id()))return new Node(240,108,18);
            if(id.equals(ShopCatalog.NO_RAIN.id()))return new Node(400,108,18);
            if(id.equals(ShopCatalog.PERMANENT_DAY.id()))return new Node(560,108,18);
            if(id.equals(ShopCatalog.ADVANCED_AE2_STARTER.id()))return new Node(400,326,18);
            int custom=0;for(var p:d.products()){if(p.id().equals(id))break;if(p.id().startsWith("forever_skyblock:shop/custom/"))custom++;}
            return new Node(400,430+custom*105,18);
        }
        int i=0;for(var q:d.quests()){if(q.id().equals(id))break;i++;}
        return new Node(70+(i%4)*155,30+(i/4)*115,18);
    }
    public static Map<String,Node> arrange(EditorDocument d) {
        var depths=new HashMap<String,Integer>();for(var q:d.quests())depth(d,q.id(),depths);
        var lanes=new HashMap<Integer,Integer>();var starts=new HashMap<Integer,Integer>();var sizes=new TreeMap<Integer,Integer>();
        for(int level:depths.values())sizes.merge(level,1,Integer::sum);int top=35;
        for(var entry:sizes.entrySet()){starts.put(entry.getKey(),top);top+=((entry.getValue()+15)/16)*115+35;}
        var result=new LinkedHashMap<String,Node>(d.nodes());
        for(var q:d.quests()){int level=depths.get(q.id()),lane=lanes.merge(level,1,Integer::sum)-1;result.put(q.id(),new Node(60+(lane%16)*155,starts.get(level)+(lane/16)*115,18));}
        return result;
    }
    private static int depth(EditorDocument d,String id,Map<String,Integer> cache){var old=cache.get(id);if(old!=null)return old;int value=0;for(String parent:d.quest(id).requires())value=Math.max(value,1+depth(d,parent,cache));cache.put(id,value);return value;}
}
