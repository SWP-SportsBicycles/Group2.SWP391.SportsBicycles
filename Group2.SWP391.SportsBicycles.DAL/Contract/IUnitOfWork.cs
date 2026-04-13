using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Group2.SWP391.SportsBicycles.DAL.Contract
{
    public interface IUnitOfWork
    {

        public Task<int> SaveChangeAsync();
        Task BeginTransactionAsync();
        Task CommitAsync();
        Task RollbackAsync();
        void ClearChangeTracker();
        DbContext GetDbContext();
    }
}
