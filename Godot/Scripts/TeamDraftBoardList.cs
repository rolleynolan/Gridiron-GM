using Godot;

[GlobalClass]
public partial class TeamDraftBoardList : ItemList
{
    [Signal]
    public delegate void ProspectDroppedEventHandler(string prospectId, string targetProspectId, bool insertAfter);

    public override Variant _GetDragData(Vector2 atPosition)
    {
        var index = GetItemAtPosition(atPosition, true);
        if (!TryReadProspect(index, out var prospectId))
            return default;

        var preview = new Label { Text = $"  {GetItemText(index)}  " };
        preview.AddThemeFontSizeOverride("font_size", 13);
        SetDragPreview(preview);
        return new Godot.Collections.Dictionary { ["prospect_id"] = prospectId };
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data)
    {
        if (data.VariantType != Variant.Type.Dictionary)
            return false;
        var source = data.AsGodotDictionary();
        var sourceId = source.TryGetValue("prospect_id", out var sourceValue) ? sourceValue.AsString() : "";
        var targetIndex = GetItemAtPosition(atPosition, true);
        return TryReadProspect(targetIndex, out var targetId)
            && !string.IsNullOrWhiteSpace(sourceId)
            && !sourceId.Equals(targetId, System.StringComparison.OrdinalIgnoreCase);
    }

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        if (!_CanDropData(atPosition, data))
            return;
        var source = data.AsGodotDictionary();
        var targetIndex = GetItemAtPosition(atPosition, true);
        TryReadProspect(targetIndex, out var targetId);
        var targetRect = GetItemRect(targetIndex);
        var insertAfter = atPosition.Y >= targetRect.Position.Y + targetRect.Size.Y / 2f;
        EmitSignal(SignalName.ProspectDropped, source["prospect_id"].AsString(), targetId, insertAfter);
    }

    private bool TryReadProspect(int index, out string prospectId)
    {
        prospectId = "";
        if (index < 0 || index >= ItemCount)
            return false;
        var metadata = GetItemMetadata(index);
        if (metadata.VariantType != Variant.Type.String)
            return false;
        prospectId = metadata.AsString();
        return !string.IsNullOrWhiteSpace(prospectId);
    }
}
