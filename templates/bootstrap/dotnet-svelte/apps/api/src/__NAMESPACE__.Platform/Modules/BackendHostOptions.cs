namespace __NAMESPACE__.Platform.Modules;

/// <summary>
/// The host choices the platform builds from: whether the store creates its
/// schema on startup. Development and disposable databases create schema
/// automatically; every other host expects a migration step to have run.
/// </summary>
/// <param name="AutoCreateSchemas">True when the host creates the store schema on startup.</param>
public sealed record BackendHostOptions(bool AutoCreateSchemas);
