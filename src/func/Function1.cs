using CloudNative.CloudEvents;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Extensions.Dapr;
using Microsoft.Extensions.Logging;

namespace func;

public static class Function1
{
	[Function("Function1")]
	public static void Run(
		[DaprTopicTrigger("%PubSubName%", Topic = "a")] CloudEvent subEvent,
		FunctionContext functionContext
	)
	{
		var log = functionContext.GetLogger("Function1");
		log.LogInformation($"Message Received: {subEvent.Data}");
	}

}