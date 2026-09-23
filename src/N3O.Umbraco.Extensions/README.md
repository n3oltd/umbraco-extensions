# N3O.Umbraco.Extensions

The base library every other N3O Umbraco package builds on. Its central piece is the `Composer` base
class, whose `RegisterAll` scans the assemblies that `OurAssemblies` recognises as ours and registers
every type matching a predicate, so a package contributes behaviour by declaring a type rather than
by editing a registration list.

Around that it provides the strongly-typed content model, where a class deriving from
`UmbracoContent<T>` maps its properties onto Umbraco property aliases and is fetched through
`IContentCache`; the lookup framework, whose values come from static sets, Umbraco content or a
remote API behind one `ILookups` interface; page and block view models; money, currency and foreign
exchange primitives; and the extension methods the rest of the estate shares.

`OurAssemblies.Configure` must run before anything resolves types, which is why the host calls it
first. Types carrying `[Experimental]` are skipped by `RegisterAll` unless the matching dev flag is
set, so an experimental contribution compiles and ships without being wired up.
