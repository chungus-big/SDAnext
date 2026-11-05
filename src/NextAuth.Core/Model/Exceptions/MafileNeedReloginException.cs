using NextAuth.Core.Model.Entities;

namespace NextAuth.Core.Model.Exceptions;

public class MafileNeedReloginException(Mafile mafile) : Exception
{
    public Mafile Mafile { get; } = mafile;
}