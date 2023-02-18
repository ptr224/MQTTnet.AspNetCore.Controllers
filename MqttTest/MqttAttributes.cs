using MQTTnet.AspNetCore.Controllers;

namespace MqttTest;

internal abstract class ActionFilterAttribute : MqttActionFilterAttribute
{
    private readonly int _value;

    public ActionFilterAttribute(int value)
    {
        _value = value;
    }

    public override async ValueTask OnActionAsync(ActionContext context, MqttActionFilterDelegate next)
    {
        var logger = context.Services.GetRequiredService<ILogger<ActionFilterAttribute>>();

        if (context.Parameters.TryGetValue("serial", out var serial))
        {
            logger.LogDebug("Action param serial = {serial}", serial);
        }

        logger.LogDebug("Filter {value} begin", _value);
        await next();
        logger.LogDebug("Filter {value} end", _value);
    }
}

internal abstract class StringModelBinderAttribute : MqttModelBinderAttribute
{
    private readonly int _value;

    public StringModelBinderAttribute(int value)
    {
        _value = value;
    }

    public override ValueTask BindModelAsync(ModelBindingContext context)
    {
        var logger = context.Services.GetRequiredService<ILogger<StringModelBinderAttribute>>();

        if (context.Value.Type == typeof(string))
        {
            if (_value == 1)
                context.Result = ModelBindingResult.Success(context.Value.Value);

            logger.LogDebug("Model binder {value}: Value = {ctx.value} - IsSet = {ctx.isset}", _value, context.Value.Value, context.Result.IsSet);
        }
        else
        {
            logger.LogWarning("Not a string");
        }

        return ValueTask.CompletedTask;
    }
}

// ================================================================

internal class ActionFilter1Attribute : ActionFilterAttribute
{
    public ActionFilter1Attribute() : base(1)
    { }
}

internal class ActionFilter2Attribute : ActionFilterAttribute
{
    public ActionFilter2Attribute() : base(2)
    { }
}

internal class ActionFilter3Attribute : ActionFilterAttribute
{
    public ActionFilter3Attribute() : base(3)
    { }
}

internal class ActionFilter4Attribute : ActionFilterAttribute
{
    public ActionFilter4Attribute() : base(4)
    { }
}

internal class ActionFilter5Attribute : ActionFilterAttribute
{
    public ActionFilter5Attribute() : base(5)
    { }
}

// ================================================================

internal class StringModelBinder1Attribute : StringModelBinderAttribute
{
    public StringModelBinder1Attribute() : base(1)
    { }
}

internal class StringModelBinder2Attribute : StringModelBinderAttribute
{
    public StringModelBinder2Attribute() : base(2)
    { }
}

internal class StringModelBinder3Attribute : StringModelBinderAttribute
{
    public StringModelBinder3Attribute() : base(3)
    { }
}

internal class StringModelBinder4Attribute : StringModelBinderAttribute
{
    public StringModelBinder4Attribute() : base(4)
    { }
}

internal class StringModelBinder5Attribute : StringModelBinderAttribute
{
    public StringModelBinder5Attribute() : base(5)
    { }
}

internal class StringModelBinder6Attribute : StringModelBinderAttribute
{
    public StringModelBinder6Attribute() : base(6)
    { }
}

internal class StringModelBinder7Attribute : StringModelBinderAttribute
{
    public StringModelBinder7Attribute() : base(7)
    { }
}