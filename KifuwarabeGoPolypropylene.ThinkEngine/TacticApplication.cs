namespace KifuwarabeGoPolypropylene.ThinkEngine;

// Records actual selected moves, not hypothetical candidates or a proof of success.
public sealed record TacticApplication(string Id, int MoveNumber, int Color, int Point);
