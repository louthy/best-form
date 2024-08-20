namespace BestForm.Model;

/// <summary>
/// Name / Value attribute
/// </summary>
public record Attr(string Name, string Value)
{
    public static Attr id(string id) =>
        new ("id", id);
    
    public static Attr @class(string value) =>
        new ("class", value);
    
    public static Attr src(string value) =>
        new ("src", value);
    
    public static Attr integrity(string value) =>
        new ("integrity", value);
    
    public static Attr crossorigin(string value) =>
        new ("crossorigin", value);
    
    public static Attr referrerpolicy(string value) =>
        new ("referrerpolicy", value);
    
    public static Attr href(string value) =>
        new ("href", value);

    public static Attr colspan(int x) =>
        new ("colspan", x.ToString());

    public override string ToString() =>
        $"{Name}=\"{Value}\"";
}
