namespace LiteSpec.Ui;

/// <summary>
///     The dependency boundary checks of the UI validator: no second
///     general-purpose visual system anywhere in the workspace.
/// </summary>
/// <follows>litespec/frontend/ui#select-one-visual-authority</follows>
sealed class DependencyBoundaryChecks
{
    /// <summary>The state of the run.</summary>
    readonly UiSession session;

    /// <summary>The second visual systems the boundary refuses.</summary>
    static readonly string[] VisualPackages =
    [
        "@chakra-ui/react",
        "@fluentui/react",
        "@blueprintjs/core",
        "@mantine/core",
        "@mui/material",
        "@radix-ui/themes",
        "@skeletonlabs/skeleton",
        "antd",
        "bootstrap",
        "daisyui",
        "flowbite-svelte",
        "material-ui",
        "react-bootstrap",
    ];

    /// <summary>
    ///     Initializes a new instance of the <see cref="DependencyBoundaryChecks"/> class.
    /// </summary>
    /// <param name="session">The state of the run.</param>
    public DependencyBoundaryChecks(UiSession session)
    {
        this.session = session;
    }

    // A workspace hoists dependencies, so a second visual system declared at the
    // repository root or in another shared package reaches every frontend without
    // appearing in any frontend manifest.
    /// <summary>
    ///     Every package manifest the boundary reads.
    /// </summary>
    /// <returns>The package manifest files.</returns>
    public List<string> PackageManifests()
    {
        var files = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        void Add(string file)
        {
            if (seen.Add(Path.GetFullPath(file)))
            {
                files.Add(file);
            }
        }

        var rootManifest = Path.Combine(session.Root, "package.json");
        if (File.Exists(rootManifest))
        {
            Add(rootManifest);
        }

        foreach (var directory in new[] { "packages", "apps" })
        {
            foreach (var file in FileWalk.Walk(Path.Combine(session.Root, directory), candidate => Path.GetFileName(candidate) == "package.json"))
            {
                Add(file);
            }
        }

        return files;
    }

    /// <summary>
    ///     Refuses a second general-purpose visual dependency in any manifest.
    /// </summary>
    public void ValidateDependencyBoundary()
    {
        foreach (var file in PackageManifests())
        {
            var label = session.RelativeToRoot(file);
            var packageJson = session.ReadJson(file, label);
            if (packageJson is null)
            {
                continue;
            }

            // An `optionalDependencies` entry installs when the platform allows it, and
            // an `overrides`, `pnpm.overrides`, or `resolutions` entry pins a version of
            // a package the tree resolves. Each one puts the named package in
            // `node_modules` where a component can import it, so each one is a declared
            // visual dependency for this boundary.
            var dependencies = new Dictionary<string, JsValue>(StringComparer.Ordinal);
            foreach (var key in new[] { "dependencies", "devDependencies", "peerDependencies", "optionalDependencies", "overrides", "resolutions" })
            {
                Merge(JsonRead.Child(packageJson, key));
            }

            Merge(JsonRead.Dot(JsonRead.Child(packageJson, "pnpm"), "overrides"));
            foreach (var name in VisualPackages)
            {
                if (dependencies.TryGetValue(name, out var dependency) && JsonRead.Truthy(dependency))
                {
                    session.Errors.Add($"[litespec/frontend/ui#select-one-visual-authority] {label}: second general-purpose visual dependency '{name}' requires an exception");
                }
            }

            void Merge(JsValue section)
            {
                if (section.Node is System.Text.Json.Nodes.JsonObject found)
                {
                    foreach (var property in found)
                    {
                        dependencies[property.Key] = new JsValue(true, property.Value);
                    }
                }
            }
        }
    }
}
