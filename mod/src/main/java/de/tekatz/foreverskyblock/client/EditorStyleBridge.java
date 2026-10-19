package de.tekatz.foreverskyblock.client;

import de.tekatz.foreverskyblock.editor.EditorDocument;

/** Tiny public bridge so mixins can scope native MenuArt rendering without making the renderer API public. */
public final class EditorStyleBridge {
    private EditorStyleBridge() { }
    public static EditorDocument.Style push(String id){return MenuArt.pushStyle(EditorClient.style(id));}
    public static void restore(EditorDocument.Style previous){MenuArt.pushStyle(previous);}
}
