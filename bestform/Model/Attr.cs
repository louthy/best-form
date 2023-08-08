namespace BestForm.Model;

/// <summary>
/// Name / Value attribute
/// </summary>
public record Attr(string Name, string Value)
{
    public static Attr id(string id) =>
        new Attr("id", id);
    
    public static Attr @class(string value) =>
        new Attr("class", value);
    
    public static Attr href(string value) =>
        new Attr("href", value);

    public static Attr colspan(int x) =>
        new Attr("colspan", x.ToString());

    public override string ToString() =>
        $"{Name}=\"{Value}\"";
}
