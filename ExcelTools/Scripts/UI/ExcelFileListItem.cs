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
    private string _LockByMe;
    public string LockByMe
    {
        get { return _LockByMe; }
        set
        {
            _LockByMe = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("LockByMe"));
        }
    }
    //ÊÇ·ñ´¦ÓÚ±à¼­×´Ì¬
    public bool IsEditing { get; set; }
    public string ClientServer { get; set; }
    public string FilePath { get; set; }
}