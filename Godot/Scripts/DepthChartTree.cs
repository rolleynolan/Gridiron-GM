using Godot;

[GlobalClass]
public partial class DepthChartTree : Tree
{
    [Signal]
    public delegate void PlayerDroppedEventHandler(string position, string playerId, string targetPlayerId, bool insertAfter);

    public override Variant _GetDragData(Vector2 atPosition)
    {
        var item = GetItemAtPosition(atPosition);
        if (!TryReadPlayer(item, out var position, out var playerId, out var name))
            return default;

        var preview = new Label { Text = $"  {name}  ·  {position}  " };
        preview.AddThemeFontSizeOverride("font_size", 13);
        SetDragPreview(preview);
        return new Godot.Collections.Dictionary
        {
            ["position"] = position,
            ["player_id"] = playerId,
        };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
            return false;
        var source = data.AsGodotDictionary();
        var sourcePosition = source.TryGetValue("position", out var positionValue) ? positionValue.AsString() : "";
        var sourceId = source.TryGetValue("player_id", out var idValue) ? idValue.AsString() : "";
        var target = GetItemAtPosition(atPosition);
        return TryReadPlayer(target, out var targetPosition, out var targetId, out _)
            && !string.IsNullOrWhiteSpace(sourceId)
            && sourcePosition.Equals(targetPosition, System.StringComparison.OrdinalIgnoreCase)
            && !sourceId.Equals(targetId, System.StringComparison.OrdinalIgnoreCase);
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (!_CanDropData(atPosition, data))
            return;
        var source = data.AsGodotDictionary();
        var target = GetItemAtPosition(atPosition);
        TryReadPlayer(target, out _, out var targetId, out _);
        var targetRect = GetItemAreaRect(target, 0);
        var insertAfter = atPosition.Y >= targetRect.Position.Y + targetRect.Size.Y / 2f;
        EmitSignal(SignalName.PlayerDropped, source["position"].AsString(), source["player_id"].AsString(), targetId, insertAfter);
    }

    private static bool TryReadPlayer(TreeItem item, out string position, out string playerId, out string name)
    {
        position = "";
        playerId = "";
        name = "";
        if (item == null)
            return false;
        var metadata = item.GetMetadata(0);
        if (metadata.VariantType != Variant.Type.Dictionary)
            return false;
        var row = metadata.AsGodotDictionary();
        position = row.TryGetValue("position", out var positionValue) ? positionValue.AsString() : "";
        playerId = row.TryGetValue("player_id", out var idValue) ? idValue.AsString() : "";
        name = row.TryGetValue("name", out var nameValue) ? nameValue.AsString() : "Player";
        return !string.IsNullOrWhiteSpace(position) && !string.IsNullOrWhiteSpace(playerId);
    }
}
