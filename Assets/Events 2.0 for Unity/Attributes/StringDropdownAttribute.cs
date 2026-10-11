using System;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
public class StringDropdownAttribute : Attribute
{
    public string[] MethodNames { get; }
    public string[][] StaticOptions { get; }
    public bool SortOptions { get; set; } = false;
    public bool EnableSearch { get; set; } = false;
    public string DropdownTitle { get; set; } = "Select an Option";

    /// <summary>
    /// Specify multiple method names for dynamically fetching options for each parameter.
    /// </summary>
    public StringDropdownAttribute(params string[] methodNames)
    {
        MethodNames = methodNames;
    }

    /// <summary>
    /// Specify static options for each parameter.
    /// </summary>
    public StringDropdownAttribute(params string[][] staticOptions)
    {
        StaticOptions = staticOptions;
    }
}