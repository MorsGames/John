namespace John;

internal abstract class Platform
{
    public abstract string Name { get; }
    public abstract string DisplayName { get; }
    public virtual string Family => Name;
    public abstract PlatformCategory Category { get; }
    public virtual IReadOnlyList<Host> SupportedHosts => Enum.GetValues<Host>();
    public virtual bool BuildsThroughWSL => false;
    public virtual bool SupportsCurrentHost => SupportedHosts.Contains(Hosts.Current);


    // 1. Find the SDKs, prepare the environment
    public virtual void PrepareEnvironment(BuildContext context)
    {
    }

    // 2. What goes into the CMake files
    public abstract void ConfigureToolchain(Toolchain toolchain, BuildContext context);

    public virtual void ConfigureGame(GameTarget target, Game game, BuildContext context)
    {
    }

    // 3. Where a game's files end up
    public abstract GameLayout GetLayout(PlatformPaths paths, Game game, Configuration configuration);

    // 4. After all the resources are staged
    public virtual void AfterStage(BuildContext context, Game game)
    {
    }

    // 5. After linking, turn what the linker made into the executable/library
    public virtual void Finalize(FinalizeContext context)
    {
    }

    // 6. Run our beautiful result
    public virtual void Run(BuildContext context, Game game, IReadOnlyList<string> args)
    {
        var program = context.GetLayout(game).ProgramFile;
        Shell.Command(program, args, context.Environment).WithWorkingDirectory(Path.GetDirectoryName(program)!).StartAndWait(game.Name);
    }

    // 6. Or pack it up for the others
    public abstract void Package(BuildContext context, Game game, string destination);

    // Other
    public override string ToString()
    {
        return Name;
    }

    protected static void AddClangFlags(Toolchain toolchain)
    {
        toolchain.SetCompilerFlags(ConfigurationBase.Debug, "-O0 -g");
        toolchain.SetCompilerFlags(ConfigurationBase.Release, "-O3 -DNDEBUG");
        toolchain.SetCompilerFlags(ConfigurationBase.RelWithDebInfo, "-O3 -g -fno-omit-frame-pointer -DNDEBUG");
    }
}
