using Autofac;

namespace ActDim.AppRegistry.Repo
{
    public class RepoModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<UserRepo>()
                .As<IUserRepo>()
                .SingleInstance();
        }
    }
}