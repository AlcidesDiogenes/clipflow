namespace ClipFlow.App.Models;

public enum ClipKind
{
    Text = 0,
    Image = 1,
    Files = 2
}

public enum ClipSubKind
{
    PlainText = 0,
    Url = 1,
    Email = 2,
    HexColor = 3,
    FilePath = 4,
    Code = 5
}

public enum ShortcutChoice
{
    CtrlShiftV = 0,
    AltV = 1,
    CtrlAltV = 2,
    CtrlShiftC = 3,
    AltC = 4
}

public enum AppTheme
{
    Dark = 0,
    Light = 1
}

public enum EdgeTriggerMode
{
    Disabled = 0,
    LeftOnly = 1,
    RightOnly = 2,
    BothEdges = 3
}

public enum WindowSpawnPosition
{
    NearCursor = 0,
    CenterScreen = 1,
    LeftEdge = 2,
    RightEdge = 3,
    NearestEdge = 4,
    BottomRight = 5,
    TopRight = 6,
    TopLeft = 7,
    BottomLeft = 8
}
