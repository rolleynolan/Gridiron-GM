using Godot;

[GlobalClass]
public partial class LiveGameFieldView : Control
{
    private int _yardLine = 50;
    private bool _homePossession;

    public void SetPlay(int yardLine, bool homePossession)
    {
        _yardLine = Mathf.Clamp(yardLine, 0, 100);
        _homePossession = homePossession;
        QueueRedraw();
    }

    public override void _Draw()
    {
        var bounds = new Rect2(Vector2.Zero, Size);
        DrawRect(bounds, new Color("123c26"));
        var field = new Rect2(34, 28, Mathf.Max(120, Size.X - 68), Mathf.Max(120, Size.Y - 56));
        DrawRect(field, new Color("24723b"));
        DrawRect(field, new Color("d6e4cf"), false, 2);

        for (var marker = 0; marker <= 10; marker++)
        {
            var x = field.Position.X + (field.Size.X * marker / 10f);
            DrawLine(new Vector2(x, field.Position.Y), new Vector2(x, field.End.Y), new Color(1, 1, 1, marker is 0 or 10 ? 0.75f : 0.32f), marker is 0 or 10 ? 2 : 1);
            if (marker is > 0 and < 10)
            {
                var number = marker <= 5 ? marker * 10 : (10 - marker) * 10;
                DrawString(ThemeDB.FallbackFont, new Vector2(x - 11, field.Position.Y + 24), number.ToString(), HorizontalAlignment.Left, -1, 15, new Color(1, 1, 1, 0.65f));
                DrawString(ThemeDB.FallbackFont, new Vector2(x - 11, field.End.Y - 10), number.ToString(), HorizontalAlignment.Left, -1, 15, new Color(1, 1, 1, 0.65f));
            }
        }

        var ballX = field.Position.X + (field.Size.X * _yardLine / 100f);
        DrawLine(new Vector2(ballX, field.Position.Y), new Vector2(ballX, field.End.Y), new Color("f0ba27"), 3);
        var offenseColor = _homePossession ? new Color("d5a62c") : new Color("dbe6ee");
        var defenseColor = _homePossession ? new Color("dbe6ee") : new Color("d5a62c");
        for (var index = 0; index < 7; index++)
        {
            var y = field.Position.Y + 70 + (index * Mathf.Max(20, (field.Size.Y - 140) / 6));
            DrawCircle(new Vector2(ballX - 16, y), 6, offenseColor);
            DrawCircle(new Vector2(ballX + 26, y), 6, defenseColor);
        }
        DrawCircle(new Vector2(ballX, field.GetCenter().Y), 4, new Color("6e301c"));
        DrawString(ThemeDB.FallbackFont, new Vector2(field.Position.X + 12, field.End.Y - 18), "AUTHORITATIVE PLAYBACK POSITION", HorizontalAlignment.Left, -1, 13, new Color(1, 1, 1, 0.72f));
    }
}
