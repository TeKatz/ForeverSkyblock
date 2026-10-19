package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.editor.EditorDocument;

/** Tiny public bridge so mixins can scope native MenuArt rendering without making the renderer API public. */
public final class EditorStyleBridge {
    private EditorStyleBridge() { }
    public static EditorDocument.Style push(String id){return MenuArt.pushStyle(EditorClient.style(id));}
    /** Native widgets already apply color/font scaling; this scope adds only authored effects/shadow. */
    public static EditorDocument.Style pushEffects(String id){
        var s=EditorClient.style(id);var fx=new EditorDocument.Style(0,0,0,0,EditorDocument.Anchor.START,EditorDocument.Anchor.START,1,1,"","","",false,"","","",s.effect(),s.effectColor(),s.effectStrength(),s.textShadow());
        return MenuArt.pushStyle(fx);
    }
    public static void restore(EditorDocument.Style previous){MenuArt.pushStyle(previous);}
}
