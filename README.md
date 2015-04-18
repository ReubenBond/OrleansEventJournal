# OrleansEventJournal
Simple calculator demonstrating a Web-based console &amp; an Event Sourcing implementation for Project Orleans.

Repos without any explanation suck, so here goes.

This is a demo project, demonstrating some things which we've been developing.

## Console
The first thing it shows is a Web-based terminal for Orleans. The idea is that you can perform arbitrary operations on actors. The console uses runtime code generation to create an efficient interface with the actor system.

In the console, type `to <actor kind>/<actor id>` to change scope to a new actor - tab-completion helps you discover the actor type names. The names are derived from the actor interface name, by default. Use the `Actor` attribute to override actor names.
Only `Guid` ids are supported (i.e, actors deriving from `IGrainWithGuidKey`).

Once an actor has been set using the `to` command, use tab-completion to discover the actor methods. An example might be `setDisplayName bob`, which would correspond to a `SetDisplayName(string name)` method on an actor. Command names can be overridden using the (poorly named) `Event` attribute. Complex commands (which require JSON object, for example) can be input using the `js` mode.

The console POSTs command to a very basic "invoke" endpoint (ASP.NET Web API self-hosted via OWIN). The demo avoids authentication and authorization. That stuff has been implemented in our private code, but didn't apply to the demo. You can still see some auth code in `console.js`.

## Event Sourcing
The other thing which we demonstrate here is a terse Event Sourcing API. The API is based around a single consumer-facing method with various overloads to handle async, etc:
```c#
protected Task Event(Action validate, Func<IActorInterface, Task> emit,  Action apply);
```

The event sourcing API is conceptually explained in [this comment](https://github.com/dotnet/orleans/issues/343#issuecomment-94103353), where I've tried to clarify some misunderstandings and lay out the future plans.
