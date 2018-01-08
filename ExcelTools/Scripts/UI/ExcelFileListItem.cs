using System.ComponentModel;

class ExcelFileListItem : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler PropertyChanged;

    public string Name { get; set; }
    private string _Status;
    public string Status {
        get { return _Status; }
        set {
            _Status = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Status"));
        } }
    public string ClientServer { get; set; }
    public string FilePath { get; set; }
}