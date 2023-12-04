using Bulky.DataAccess.Data;
using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Bulky.DataAccess.Repository
{
    public class OrderDetailRepository : Repository<OrderDetail>, IOrderDetailRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderDetailRepository(ApplicationDbContext dbContext) : base(dbContext)
        {
            _db = dbContext;
        }
        public void Save()
        {
            _db.SaveChanges();
        }

        public void Update(OrderDetail orderDetail)
        {
            _db.OrderDetails.Update(orderDetail);
        }
    }
}
