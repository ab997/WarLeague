using System;
using System.Collections.Generic;
using System.Text;
using WarLeague.Core.Data;

namespace WarLeague.Test
{
    /// <summary>
    /// Base class for tests that modify data. Provides automatic transaction rollback
    /// so each test starts with fresh seeded data.
    /// </summary>
    public abstract class TransactionalTestBase : IClassFixture<DatabaseFixtureSeeded>, IDisposable
    {
        protected readonly DatabaseFixtureSeeded Fixture;
        protected readonly WarLeagueDbContext Context;
        private readonly Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction _transaction;

        protected TransactionalTestBase(DatabaseFixtureSeeded fixture)
        {
            Fixture = fixture;
            Context = fixture.CreateContext();
            _transaction = Context.Database.BeginTransaction();
        }

        public void Dispose()
        {
            _transaction?.Rollback();
            _transaction?.Dispose();
            Context?.Dispose();
        }
    }
}
