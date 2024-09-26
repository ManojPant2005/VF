namespace UI.Jobs.Services
{
    public interface IDatabaseService
    {
        void ProcessSmsMessages();
        void UpdateSmsTransmittedOn(int smsId);
    }
}
