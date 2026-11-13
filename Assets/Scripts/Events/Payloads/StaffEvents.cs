namespace GameLike.Core.Events
{
    /// <summary>
    /// Raised when an employee is hired from the talent market. High fan-out:
    /// economy (payroll), office view (spawn desk), valuation and UI react.
    /// Carries primitive data only — the Events layer sits below State and cannot
    /// reference EmployeeInstance.
    /// </summary>
    public readonly struct EmployeeHiredEvent : IGameEvent
    {
        public readonly string EmployeeId;
        public readonly string EmployeeName;
        public readonly string Specialty;

        public EmployeeHiredEvent(string employeeId, string employeeName, string specialty)
        {
            EmployeeId = employeeId;
            EmployeeName = employeeName;
            Specialty = specialty;
        }

        public EventCategory Category => EventCategory.Employee;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"Hired {EmployeeName} ({Specialty})";
    }

    /// <summary>
    /// Raised when an employee leaves the company (fired, quit, or poached).
    /// </summary>
    public readonly struct EmployeeLeftEvent : IGameEvent
    {
        public readonly string EmployeeId;
        public readonly string EmployeeName;
        public readonly LeaveReason Reason;

        public EmployeeLeftEvent(string employeeId, string employeeName, LeaveReason reason)
        {
            EmployeeId = employeeId;
            EmployeeName = employeeName;
            Reason = reason;
        }

        public EventCategory Category => EventCategory.Employee;
        public EventSeverity Severity => EventSeverity.Warning;
        public string Describe() => $"{EmployeeName} left ({Reason})";
    }

    /// <summary>Raised when an employee is promoted a seniority level by accumulated XP (GDD 7.3).</summary>
    public readonly struct EmployeePromotedEvent : IGameEvent
    {
        public readonly string EmployeeId;
        public readonly string EmployeeName;
        public readonly string NewSeniority;

        public EmployeePromotedEvent(string employeeId, string employeeName, string newSeniority)
        {
            EmployeeId = employeeId;
            EmployeeName = employeeName;
            NewSeniority = newSeniority;
        }

        public EventCategory Category => EventCategory.Employee;
        public EventSeverity Severity => EventSeverity.Info;
        public string Describe() => $"{EmployeeName} promoted to {NewSeniority}";
    }

    /// <summary>Raised when the rotating candidate pool is refreshed (GDD 7.2).</summary>
    public readonly struct TalentMarketRefreshedEvent : IGameEvent
    {
        public readonly int CandidateCount;

        public TalentMarketRefreshedEvent(int candidateCount)
        {
            CandidateCount = candidateCount;
        }

        public EventCategory Category => EventCategory.Employee;
        public EventSeverity Severity => EventSeverity.Verbose;
        public string Describe() => $"Talent market refreshed — {CandidateCount} candidates";
    }

    /// <summary>Why an employee left the company.</summary>
    public enum LeaveReason
    {
        Fired,
        Quit,
        Burnout,
        Poached
    }
}
