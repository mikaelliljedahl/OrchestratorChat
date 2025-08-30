using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace OrchestratorChat.Data.Repositories;

public class Repository<TEntity> : IRepository<TEntity> where TEntity : class
{
    protected readonly OrchestratorDbContext _context;
    protected readonly DbSet<TEntity> _dbSet;
    
    public Repository(OrchestratorDbContext context)
    {
        _context = context;
        _dbSet = context.Set<TEntity>();
    }
    
    public virtual async Task<TEntity?> GetByIdAsync(string id)
    {
        return await _dbSet.FindAsync(id);
    }
    
    public virtual async Task<IEnumerable<TEntity>> GetAllAsync()
    {
        return await _dbSet.ToListAsync();
    }
    
    public virtual async Task<IEnumerable<TEntity>> FindAsync(
        Expression<Func<TEntity, bool>> predicate)
    {
        return await _dbSet.Where(predicate).ToListAsync();
    }
    
    public virtual async Task<TEntity> AddAsync(TEntity entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();
        return entity;
    }
    
    public virtual async Task UpdateAsync(TEntity entity)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync();
    }
    
    public virtual async Task DeleteAsync(string id)
    {
        var entity = await GetByIdAsync(id);
        if (entity != null)
        {
            _dbSet.Remove(entity);
            await _context.SaveChangesAsync();
        }
    }
    
    public virtual async Task<bool> ExistsAsync(string id)
    {
        return await _dbSet.FindAsync(id) != null;
    }
    
    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null)
    {
        return predicate == null 
            ? await _dbSet.CountAsync()
            : await _dbSet.CountAsync(predicate);
    }
}