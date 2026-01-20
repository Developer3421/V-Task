using System.ComponentModel;

namespace V_Task.Models;

public class CpuCoreInfo : INotifyPropertyChanged
{
    private string _coreName = string.Empty;
    private float _usage;
    private string _usageText = string.Empty;
    
    public string CoreName 
    { 
        get => _coreName; 
        set { _coreName = value; OnPropertyChanged(nameof(CoreName)); } 
    }
    
    public float Usage 
    { 
        get => _usage; 
        set { _usage = value; OnPropertyChanged(nameof(Usage)); } 
    }
    
    public string UsageText 
    { 
        get => _usageText; 
        set { _usageText = value; OnPropertyChanged(nameof(UsageText)); } 
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;
    
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
