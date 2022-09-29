namespace DoToo.Repositories;

using DoToo.Models;

public interface ITodoItemRepository
{
    event EventHandler<TodoItem> OnItemAdded; event EventHandler<TodoItem> OnItemUpdated;

    Task<List<TodoItem>> GetItems(); Task AddItem(TodoItem item); Task UpdateItem(TodoItem item); Task AddOrUpdate(TodoItem item);
}
