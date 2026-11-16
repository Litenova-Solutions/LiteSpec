# Cross-Module Coupling Practice

LiteSpec 6.0.0 organizes a modular monolith by module, aggregate, and use case. It lets a use case run another use case inline, publish its own integration events, and read another module through a read port. This research record compares that shape with outside practice and with what one large consumer built under it. The record read both on 14 November 2026. Each allowance looks small on its own, but together they grow shared folders and couple modules that the rules keep apart.

## Findings

Outside practice keeps coupling inside a slice and out of the space between slices. Jimmy Bogard's 2018 article that named vertical slice architecture states the aim as minimal coupling between slices and maximal coupling inside one. A slice that names another slice's request type is coupled to that slice's reasons to change. The record gives no confidence for this claim.

A module learns another module's state without asking for it. Martin Fowler's 2017 article on event-driven systems names the pattern event-carried state transfer. A receiver keeps its own copy of the data it needs, filled from the events the owner publishes, and stops calling the owner. Fowler names the cost: the receiver maintains that copy and accepts that it lags the owner. The record gives no confidence for this claim.

A port is a seam the core declares for something outside it. Alistair Cockburn's 2005 article on ports and adapters, also called hexagonal architecture, places a port where the application meets a driver or a driven system. Adapters change behind it. A repository and an outside system fit that shape. A reader that one module calls to fetch another module's current state is an in-process service call. It runs between two parts of one core, not as a seam to the outside. The record gives no confidence for this claim.

A published modular monolith reference separates the two kinds of message. The modular-monolith-with-ddd repository keeps domain events inside the module that raised them. It crosses module boundaries with integration events processed in their own transactions. It runs follow-on work inside a module as internal commands from its own processing. No module sends another module's command. The record gives no confidence for this claim.

One large consumer of LiteSpec 5.2.0, with 43 modules and 1,541 use-case folders, measured what the allowances built, before its 6.0.0 upgrade. About 270 files in aggregate `Application/` folders are use-case messages and results outside their folders, because a second use case named them. 214 files stood in module `Contracts/Commands/` folders. Other modules named 104 commands outside their module. 101 refusal types stood in `Contracts/Exceptions/`, and other modules threw 72 of them. The record gives no confidence for these counts, which come from one consumer.

The same consumer showed 57 inline edges in 47 use-case records, and 33 command handlers used a mediator. Running a command inside another command's transaction needs a nested dispatch scope with two ambient values and a per-message aggregate cache. It also needs reload methods that avoid stale reads. 69 handlers published their own integration events, which they built from the aggregate rather than from the fact the act raised. The record gives no confidence for these counts.

Cross-module readers showed the largest coupling. 741 files of other modules named 86 reader interfaces in `Contracts/Ports/` folders. They were 274 command handlers, 148 query handlers, 114 reactions, and the rest in services and steps. The busiest reader had 18 methods, several serving their one caller. A screen that calls each owner's query answers a query handler that only shows another module's data with less coupling. The record gives no confidence for these counts.

Trigger folders split what one use case does. 232 folders named for a fact contained event handlers. 22 facts had such folders in more than one module, so a fact folder never showed every reaction to its fact. Each handler named the command of a use case in another folder, which moved those commands to shared folders. The record gives no confidence for these counts.

## Source Notes

The four outside sources are articles and a repository, read through their published text and summaries on 14 November 2026. The record took the consumer counts on the consumer's main branch on the same day, and the consumer keeps them. The LiteSpec paths are the 6.0.0 release pages this research changed.
