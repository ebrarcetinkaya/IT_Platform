namespace BtPlatform.Core.Entities;

public enum DeviceStatus { InUse, InStock, InRepair, Retired }
public enum TicketStatus { Open, InProgress, Resolved, Closed }
public enum TicketPriority { Low, Medium, High, Critical }
public enum TicketCategory { ActiveDirectory, Network, Printer, Hardware, Software, Other }
public enum PortStatus { Up, Down, ErrDisabled }
public enum AlertSeverity { Info, Warning, Critical }
public enum UserRole { Viewer, Technician, Admin }
public enum UpdateJobStatus { Pending, Downloading, Installing, Succeeded, Failed }