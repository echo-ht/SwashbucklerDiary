using MoXia.Shared;

namespace MoXia.Rcl.Services
{
    public interface IDatabaseScopeAccessor
    {
        LibraryKind? Current { get; }

        IDisposable Use(LibraryKind library);
    }

    public sealed class DatabaseScopeAccessor : IDatabaseScopeAccessor
    {
        private static readonly AsyncLocal<LibraryKind?> current = new();

        public LibraryKind? Current => current.Value;

        public IDisposable Use(LibraryKind library)
        {
            LibraryKind? previous = current.Value;
            current.Value = library;
            return new Scope(previous);
        }

        private sealed class Scope(LibraryKind? previous) : IDisposable
        {
            private bool disposed;

            public void Dispose()
            {
                if (disposed)
                {
                    return;
                }

                current.Value = previous;
                disposed = true;
            }
        }
    }
}
