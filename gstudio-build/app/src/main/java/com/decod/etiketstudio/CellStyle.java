package com.decod.etiketstudio;

final class CellStyle {
    static final int ALIGN_LEFT = 0;
    static final int ALIGN_CENTER = 1;
    static final int ALIGN_RIGHT = 2;
    int fontPt = 12;
    boolean bold = true;
    boolean italic;
    boolean underline;
    int rotation;
    int alignment = ALIGN_CENTER;

    CellStyle copy() {
        CellStyle c = new CellStyle();
        c.fontPt = fontPt; c.bold = bold; c.italic = italic; c.underline = underline;
        c.rotation = rotation; c.alignment = alignment;
        return c;
    }
}
