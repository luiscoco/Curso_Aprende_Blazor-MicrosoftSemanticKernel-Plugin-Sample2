using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System.ComponentModel;
using System.Reflection;
using System.Text.Json.Serialization;

var builder = Kernel.CreateBuilder();

builder.AddAzureOpenAIChatCompletion("gpt-4o",
    "https://luiscocoaiservice.openai.azure.com/",
    "",
    "gpt-4o");

builder.Plugins.AddFromType<WidgetFactory>();

var kernel = builder.Build();

#pragma warning disable
OpenAIPromptExecutionSettings settings = new() { FunctionChoiceBehavior = FunctionChoiceBehavior.Auto() };
// Example 4. Invoke the kernel with a prompt and allow the AI to automatically invoke functions that use enumerations
Console.WriteLine(await kernel.InvokePromptAsync("Create a handy lime colored widget for me.", new(settings)));
Console.WriteLine(await kernel.InvokePromptAsync("Create a beautiful scarlet colored widget for me.", new(settings)));
Console.WriteLine(await kernel.InvokePromptAsync("Create an attractive maroon and navy colored widget for me.", new(settings)));

/// <summary>
/// A plugin that creates widgets.
/// </summary>
public class WidgetFactory
{
    [KernelFunction]
    [Description("Creates a new widget of the specified type and colors")]
    public object CreateWidget([Description("The type of widget to be created")] WidgetType widgetType, [Description("The colors of the widget to be created")] WidgetColor[] widgetColors)
    {
        // Validate colors
        var validColors = Enum.GetValues(typeof(WidgetColor)).Cast<WidgetColor>().ToList();
        var invalidColors = widgetColors.Except(validColors).ToArray();

        if (invalidColors.Any())
        {
            var availableColors = string.Join(", ", validColors.Select(c => c.GetDisplayName()));
            return $"The color(s) {string.Join(", ", invalidColors)} are not available. Please choose from the available colors: {availableColors}.";
        }

        // Create widget if all colors are valid
        var colors = string.Join('-', widgetColors.Select(c => c.GetDisplayName()).ToArray());
        return new WidgetDetails
        {
            SerialNumber = $"{widgetType}-{colors}-{Guid.NewGuid()}",
            Type = widgetType,
            Colors = widgetColors
        };
    }
}

/// <summary>
/// A <see cref="JsonConverter"/> is required to correctly convert enum values.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WidgetType
{
    [Description("A widget that is useful.")]
    Useful,

    [Description("A widget that is decorative.")]
    Decorative
}

/// <summary>
/// A <see cref="JsonConverter"/> is required to correctly convert enum values.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum WidgetColor
{
    [Description("Use when creating a red item.")]
    Red,

    [Description("Use when creating a green item.")]
    Green,

    [Description("Use when creating a blue item.")]
    Blue
}

public class WidgetDetails
{
    public string SerialNumber { get; init; }
    public WidgetType Type { get; init; }
    public WidgetColor[] Colors { get; init; }
}

public static class EnumExtensions
{
    public static string GetDisplayName(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }
}