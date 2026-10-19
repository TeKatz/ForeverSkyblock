package de.tekatz.foreverskyblock.editor;

import com.google.gson.*;
import java.util.*;
import java.util.function.Consumer;

/** Client draft history never mutates the published document. Gestures commit one undo entry. */
public final class EditorSession {
    private EditorDocument current;
    private final Deque<EditorDocument> undo=new ArrayDeque<>(),redo=new ArrayDeque<>();
    private final EditorDocument baseline;
    public EditorSession(EditorDocument current){this.current=current;baseline=current;}
    public EditorDocument current(){return current;}
    public boolean dirty(){return !current.equals(baseline);}
    public void edit(Consumer<JsonObject> change){JsonObject json=EditorCodec.JSON.toJsonTree(current).getAsJsonObject();change.accept(json);replace(EditorCodec.decode(json.toString()));}
    public void replace(EditorDocument next){if(next.equals(current))return;undo.addLast(current);while(undo.size()>100)undo.removeFirst();redo.clear();current=next;}
    public void undo(){if(!undo.isEmpty()){redo.addLast(current);current=undo.removeLast();}}
    public void redo(){if(!redo.isEmpty()){undo.addLast(current);current=redo.removeLast();}}
    public void reset(){replace(baseline);}
    public static int snap(double value,int grid,boolean enabled){return (int)Math.round(enabled?Math.round(value/grid)*grid:value);}
}
