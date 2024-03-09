using MQTTnet.AspNetCore.Controllers;

namespace MqttTest;

abstract class ActionFilterAttribute(int value) : MqttActionFilterAttribute
{
    public override async ValueTask OnActionAsync(ActionContext context, MqttActionFilterDelegate next)
    {
        var logger = context.Services.GetRequiredService<ILogger<ActionFilterAttribute>>();

        if (context.Parameters.TryGetValue("serial", out var serial))
        {
            logger.LogDebug("Action param serial = {serial}", serial);
        }

        logger.LogDebug("Filter {value} begin", value);
        await next();
        logger.LogDebug("Filter {value} end", value);
    }
}

abstract class StringModelBinderAttribute(int value) : MqttModelBinderAttribute
{
    public override ValueTask BindModelAsync(ModelBindingContext context)
    {
        var logger = context.Services.GetRequiredService<ILogger<StringModelBinderAttribute>>();

        if (context.Value.Type == typeof(string))
        {
            if (value == 1)
                context.Result = ModelBindingResult.Success(context.Value.Value);

            logger.LogDebug("Model binder {value}: Value = {ctx.value} - IsSet = {ctx.isset}", value, context.Value.Value, context.Result.IsSet);
        }
        else
        {
            logger.LogWarning("Not a string");
        }

        return ValueTask.CompletedTask;
    }
}

// ================================================================

class ActionFilter1Attribute() : ActionFilterAttribute(1);

class ActionFilter2Attribute() : ActionFilterAttribute(2);

class ActionFilter3Attribute() : ActionFilterAttribute(3);

class ActionFilter4Attribute() : ActionFilterAttribute(4);

class ActionFilter5Attribute() : ActionFilterAttribute(5);

// ================================================================

class StringModelBinder1Attribute() : StringModelBinderAttribute(1);

class StringModelBinder2Attribute() : StringModelBinderAttribute(2);

class StringModelBinder3Attribute() : StringModelBinderAttribute(3);

class StringModelBinder4Attribute() : StringModelBinderAttribute(4);

class StringModelBinder5Attribute() : StringModelBinderAttribute(5);

class StringModelBinder6Attribute() : StringModelBinderAttribute(6);

class StringModelBinder7Attribute() : StringModelBinderAttribute(7);