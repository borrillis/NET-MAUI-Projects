namespace DoToo.Repositories;

using DoToo.Models;

public class TodoItemRepository : ITodoItemRepository
{
    public event EventHandler<TodoItem> OnItemAdded; 
    public event EventHandler<TodoItem> OnItemUpdated;

    public async Task<List<TodoItem>> GetItems()
    {
        return null; // Just to make it build
    }

    public async Task AddItem(TodoItem item)
    {
    }

    public async Task UpdateItem(TodoItem item)
    {
    }

    public async Task AddOrUpdate(TodoItem item)
    {
        if (item.Id == 0)
        {
            await AddItem(item);
        }
        else
        {
            await UpdateItem(item);
        }
    }

}
