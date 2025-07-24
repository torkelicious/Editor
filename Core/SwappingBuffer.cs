namespace Editor.Core;

public class SwappingBuffer : ITextBuffer, IDisposable
{
    private const int PageSize = 64 * 1024; // 64KB pages
    private const int MaxMemoryPages = 16;   // Keep 16 pages in memory (1MB)
    
    private readonly Dictionary<int, BufferPage> pages;
    private readonly LinkedList<int> lruOrder;
    private readonly string tempDirectory;
    private readonly string sessionId;
    
    private int totalLength;
    private int position;
    private bool isDisposed;

    public int Length => totalLength;
    public int Position => position;

    public SwappingBuffer()
    {
        pages = new Dictionary<int, BufferPage>();
        lruOrder = new LinkedList<int>();
        sessionId = Guid.NewGuid().ToString("N")[..8];
        tempDirectory = Path.Combine(Path.GetTempPath(), $"sharpeditor_cache_{sessionId}");
        Directory.CreateDirectory(tempDirectory);
        
        totalLength = 0;
        position = 0;
    }

    public char this[int index]
    {
        get
        {
            if (index < 0 || index >= totalLength)
                throw new IndexOutOfRangeException();

            var (pageIndex, pageOffset) = GetPageCoordinates(index);
            var page = GetPage(pageIndex);
            return page.GetChar(pageOffset);
        }
    }

    private (int pageIndex, int pageOffset) GetPageCoordinates(int globalIndex)
    {
        return (globalIndex / PageSize, globalIndex % PageSize);
    }

    private BufferPage GetPage(int pageIndex)
    {
        if (pages.TryGetValue(pageIndex, out var page))
        {
            TouchPage(pageIndex);
            return page;
        }

        page = LoadPageFromDisk(pageIndex) ?? new BufferPage(pageIndex, PageSize);
        
        pages[pageIndex] = page;
        TouchPage(pageIndex);
        
        EvictIfNeeded();
        
        return page;
    }

    private void TouchPage(int pageIndex)
    {
        lruOrder.Remove(pageIndex);
        lruOrder.AddFirst(pageIndex);
    }

    private void EvictIfNeeded()
    {
        while (lruOrder.Count > MaxMemoryPages)
        {
            var lruPageIndex = lruOrder.Last!.Value;
            var lruPage = pages[lruPageIndex];
            
            if (lruPage.IsDirty)
            {
                SavePageToDisk(lruPage);
            }
            
            pages.Remove(lruPageIndex);
            lruOrder.RemoveLast();
        }
    }

    public void Insert(char character)
    {
        var (pageIndex, pageOffset) = GetPageCoordinates(position);
        var page = GetPage(pageIndex);
        
        if (page.IsFull)
        {
            SplitPage(pageIndex, pageOffset);
            page = GetPage(pageIndex);
        }
        
        page.Insert(pageOffset, character);
        totalLength++;
        position++;
        
        UpdatePageIndicesAfterInsert(pageIndex);
    }

    public void Insert(string text)
    {
        foreach (char c in text)
        {
            Insert(c);
        }
    }

    public void Delete(int count = 1, DeleteDirection direction = DeleteDirection.Forward)
    {
        if (count <= 0) return;

        if (direction == DeleteDirection.Forward)
        {
            // forward 
            for (int i = 0; i < count && position < totalLength; i++)
            {
                var (pageIndex, pageOffset) = GetPageCoordinates(position);
                var page = GetPage(pageIndex);
                
                page.Delete(pageOffset);
                totalLength--;
                
                if (page.Length < PageSize / 4)
                {
                    TryMergePages(pageIndex);
                }
            }
        }
        else // Backward
        {
            for (int i = 0; i < count && position > 0; i++)
            {
                position--;
                var (pageIndex, pageOffset) = GetPageCoordinates(position);
                var page = GetPage(pageIndex);
                
                page.Delete(pageOffset);
                totalLength--;
                
                if (page.Length < PageSize / 4)
                {
                    TryMergePages(pageIndex);
                }
            }
        }
    }

    public void MoveTo(int position)
    {
        this.position = Math.Max(0, Math.Min(position, totalLength));
    }

    private BufferPage? LoadPageFromDisk(int pageIndex)
    {
        var filePath = GetPageFilePath(pageIndex);
        if (!File.Exists(filePath)) return null;
        
        var data = File.ReadAllText(filePath);
        var page = new BufferPage(pageIndex, PageSize);
        page.LoadFromString(data);
        return page;
    }

    private void SavePageToDisk(BufferPage page)
    {
        var filePath = GetPageFilePath(page.Index);
        var data = page.ToString();
        File.WriteAllText(filePath, data);
        page.MarkClean();
    }

    private string GetPageFilePath(int pageIndex)
    {
        return Path.Combine(tempDirectory, $"page_{pageIndex:D6}.tmp");
    }

    private void SplitPage(int pageIndex, int splitOffset)
    {
        var currentPage = pages[pageIndex];
        var newPageIndex = GetNextAvailablePageIndex();
        
        var newPage = new BufferPage(newPageIndex, PageSize);
        
        var contentToMove = currentPage.GetContentAfter(splitOffset);
        currentPage.TruncateAfter(splitOffset);
        newPage.AppendContent(contentToMove);
        
        ShiftPageIndices(pageIndex + 1, 1);
        pages[newPageIndex] = newPage;
    }

    private void UpdatePageIndicesAfterInsert(int fromPageIndex)
    {
        var pagesToUpdate = pages.Keys.Where(k => k > fromPageIndex).ToList();
        
        foreach (var pageIndex in pagesToUpdate)
        {
            var page = pages[pageIndex];
            if (page.IsDirty)
            {
                SavePageToDisk(page);
            }
        }
    }

    private void TryMergePages(int pageIndex)
    {
        if (pageIndex == 0) return; // Can't merge first page
        
        var currentPage = pages[pageIndex];
        var previousPage = pages[pageIndex - 1];
        
        if (previousPage.Length + currentPage.Length <= PageSize)
        {
            previousPage.AppendContent(currentPage.GetAllContent());
            pages.Remove(pageIndex);
            lruOrder.Remove(pageIndex);
            
            ShiftPageIndices(pageIndex + 1, -1);
        }
    }

    private int GetNextAvailablePageIndex()
    {
        return pages.Keys.DefaultIfEmpty(-1).Max() + 1;
    }

    private void ShiftPageIndices(int fromIndex, int shift)
    {
        var pagesToShift = pages.Keys.Where(k => k >= fromIndex).OrderByDescending(k => k).ToList();
        
        foreach (var oldIndex in pagesToShift)
        {
            var page = pages[oldIndex];
            var newIndex = oldIndex + shift;
            
            pages.Remove(oldIndex);
            pages[newIndex] = page;
            page.UpdateIndex(newIndex);
            
            var node = lruOrder.Find(oldIndex);
            if (node != null)
            {
                lruOrder.Remove(node);
                lruOrder.AddLast(newIndex);
            }
        }
    }

    public void Dispose()
    {
        if (isDisposed) return;
        
        foreach (var page in pages.Values.Where(p => p.IsDirty))
        {
            SavePageToDisk(page);
        }
        
        if (Directory.Exists(tempDirectory))
        {
            Directory.Delete(tempDirectory, true);
        }
        
        isDisposed = true;
    }
}