package de.tekatz.foreverskyblock.editor;

import com.google.gson.*;
import java.util.*;

/** Field-level three-way draft reconciliation, with stable IDs for content arrays. */
public final class EditorMerge {
    public record Conflict(String path,String local,String server) { }
    public record Result(EditorDocument document,List<Conflict> conflicts) { }
    private EditorMerge() { }
    public static Result reconcile(EditorDocument base,EditorDocument local,EditorDocument server,boolean preferLocal) {
        var conflicts=new ArrayList<Conflict>();var merged=merge(EditorCodec.JSON.toJsonTree(base),EditorCodec.JSON.toJsonTree(local),EditorCodec.JSON.toJsonTree(server),"",preferLocal,conflicts).getAsJsonObject();
        merged.addProperty("revision",server.revision());return new Result(EditorCodec.decode(merged.toString()),List.copyOf(conflicts));
    }
    private static JsonElement merge(JsonElement base,JsonElement local,JsonElement server,String path,boolean preferLocal,List<Conflict> conflicts) {
        if(Objects.equals(local,base))return server;
        if(Objects.equals(server,base)||Objects.equals(local,server))return local;
        if(object(base)&&object(local)&&object(server)) {
            var keys=new LinkedHashSet<String>();server.getAsJsonObject().keySet().forEach(keys::add);local.getAsJsonObject().keySet().forEach(keys::add);var result=new JsonObject();
            for(String key:keys){var value=merge(base.getAsJsonObject().get(key),local.getAsJsonObject().get(key),server.getAsJsonObject().get(key),path+"/"+key,preferLocal,conflicts);if(value!=null)result.add(key,value);}return result;
        }
        if(keyed(base)&&keyed(local)&&keyed(server)) {
            Map<String,JsonElement> b=index(base),l=index(local),s=index(server);var ids=new LinkedHashSet<String>(s.keySet());ids.addAll(l.keySet());var result=new JsonArray();
            for(String id:ids){var value=merge(b.get(id),l.get(id),s.get(id),path+"/"+id,preferLocal,conflicts);if(value!=null)result.add(value);}return result;
        }
        if(!path.equals("/revision"))conflicts.add(new Conflict(path,String.valueOf(local),String.valueOf(server)));
        return preferLocal?local:server;
    }
    private static boolean object(JsonElement e){return e!=null&&e.isJsonObject();}
    private static boolean keyed(JsonElement e){if(e==null||!e.isJsonArray())return false;for(var v:e.getAsJsonArray())if(!v.isJsonObject()||!v.getAsJsonObject().has("id"))return false;return true;}
    private static Map<String,JsonElement> index(JsonElement e){var result=new LinkedHashMap<String,JsonElement>();for(var value:e.getAsJsonArray())result.put(value.getAsJsonObject().get("id").getAsString(),value);return result;}
    /** A rollback retains newer published IDs as disabled tombstones so no player progress is erased. */
    public static EditorDocument rollback(EditorDocument current,EditorDocument backup){
        var result=EditorCodec.JSON.toJsonTree(backup).getAsJsonObject();var now=EditorCodec.JSON.toJsonTree(current).getAsJsonObject();
        for(String collection:List.of("chapters","quests","products","contracts")){
            var array=result.getAsJsonArray(collection);var ids=index(array).keySet();
            for(var value:now.getAsJsonArray(collection)){var o=value.getAsJsonObject();if(!ids.contains(o.get("id").getAsString())){var copy=o.deepCopy();if(copy.has("enabled"))copy.addProperty("enabled",false);array.add(copy);}}
        }
        result.addProperty("revision",current.revision());return EditorCodec.decode(result.toString());
    }
}
