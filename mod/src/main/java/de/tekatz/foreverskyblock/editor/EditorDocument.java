package de.tekatz.foreverskyblock.editor;

import java.util.*;

/** Versioned authoring data. IDs, rather than list positions or display text, identify saved progress. */
public record EditorDocument(int schema, long revision, Map<String, Style> layout, List<Chapter> chapters,
                             List<Quest> quests, List<Product> products, List<Contract> contracts,
                             Map<String, Node> nodes, List<Decoration> decorations, Settings settings) {
    public static final int SCHEMA = 1, MAX_BYTES = 240_000;
    public EditorDocument {
        layout = Map.copyOf(layout); chapters = List.copyOf(chapters); quests = List.copyOf(quests);
        products = List.copyOf(products); contracts = List.copyOf(contracts); nodes = Map.copyOf(nodes);
        decorations = List.copyOf(decorations); Objects.requireNonNull(settings);
    }
    public record Text(String en, String de) {
        public String value(boolean german) { return german && !de.isBlank() ? de : en; }
    }
    public enum Anchor { START, CENTER, END }
    public record Style(int x, int y, int width, int height, Anchor anchorX, Anchor anchorY,
                        float fontScale, float itemScale, String fill, String border, String text, boolean hidden, String icon,
                        String labelEn, String labelDe) {
        public Style {
            if(icon==null)icon="";
            if(labelEn==null)labelEn="";
            if(labelDe==null)labelDe="";
        }
        public static Style defaults() { return new Style(0,0,0,0,Anchor.START,Anchor.START,1,1,"","","",false,"","",""); }
        public String label(String fallback,boolean german) {
            String value=german&&!labelDe.isBlank()?labelDe:labelEn;
            return value.isBlank()?fallback:value;
        }
    }
    public record Chapter(String id, Text title, String icon, int order) { }
    public enum TaskType { LEGACY, CRAFT, SMELT, PICKUP, KILL, DIMENSION, BREAK, PLACE, ADVANCEMENT }
    public enum Match { ALL, ANY }
    public record Task(String id, TaskType type, String target, int count) { }
    public record Quest(String id, boolean enabled, Text title, Text description, String icon, String chapter,
                        List<Task> tasks, Match taskMatch, List<String> requires, Match prerequisiteMatch,
                        int requiredTier, int rewardSp, int rewardPp) {
        public Quest { tasks=List.copyOf(tasks); requires=List.copyOf(requires); }
        public boolean legacy() { return tasks.size()==1 && tasks.getFirst().type()==TaskType.LEGACY; }
    }
    public record Product(String id, boolean enabled, Text title, Text description, String icon,
                          int requiredTier, int spCost, int ppCost, List<String> requires, List<Stack> contents) {
        public Product { requires=List.copyOf(requires); contents=List.copyOf(contents); }
    }
    public record Stack(String item, int count) { }
    public enum ContractType { RESOURCE, WITHER, DRAGON }
    public record Contract(String id, boolean enabled, Text title, Text description, String icon,
                           ContractType type, String item, int count, int rewardSp, int requiredTier) { }
    /** Coordinates belong to the content canvas; links are derived from prerequisites, never duplicate geometry. */
    public record Node(float x, float y, int radius) { }
    public enum DecorationType { TEXT, PANEL, ITEM, BAR }
    public record Decoration(String id, String page, DecorationType type, Text title, String icon,
                             String binding, int x, int y, int width, int height, String color) { }
    public record Settings(int grid, boolean snap, int animationMillis, float scrollSpeed, boolean questGraph, int cardGap, int categoryGap) { }
    public Quest quest(String id) { return quests.stream().filter(q->q.id().equals(id)).findFirst().orElse(null); }
    public Product product(String id) { return products.stream().filter(p->p.id().equals(id)).findFirst().orElse(null); }
    public Contract contract(String id) { return contracts.stream().filter(c->c.id().equals(id)).findFirst().orElse(null); }
    public EditorDocument withRevision(long value) {
        return new EditorDocument(schema,value,layout,chapters,quests,products,contracts,nodes,decorations,settings);
    }
}