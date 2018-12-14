using ClassifiedPrint.Core.Models;
using System.Collections.Generic;

namespace ClassifiedPrint.Core.Repository
{
    public interface IRepository<T> where T : class
    {
        void Add(T item);
        void Remove(int id);
        void Update(T item);
        T FindByID(int id);
        IEnumerable<T> FindAll();
    }
}
