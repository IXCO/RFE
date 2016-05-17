# ConsoleRFE

## Desclaimer
This solution is for specific needs on internal procedure of *Mundo Inmobiliario S.A*
functionality may be limited in other scenarios. 
This was created according to the specifications on the legal regulations of Mexico.

## Description
*RFE* is the digital invoice receptor and validator for all the provideers that give service

to any of the industries inside __Mundo Inmobiliario S.A & Industrias__ . 

__Basic function:__

- **Receive:** On one centralized email account.
- **Validate**: According to the regulations of SAT and industries information.
- **Register**: Mark data as pending for further use on the finance procedure.
- **Storage**: Save and backup all digital files.

## Requirements

  1. Windows OS (7 or higher)
  2. MySql Instance
  3. MySql Connector library and [Limilabs Mail library](http://www.limilabs.com/mail)
  3. Preconfigured email account.

## How to use

- Go to __Email__ class and personalize account
```C#
private static string account = "example@mymail.com";
private static string password = "SomePassword";
```
- Edit __ControladorDB__ class 
```C#
private static string server = "https://www.myserver.com";
private static string username = "myuser";
private static string password = "SomePasword";
private static string database = "mydatabase";
```
- Compile and create

## Things to notice

- Structure of the database would most likely be different, therefore the queries will need to be updated.
- Policies for reception may vary, according to specification.
- Attach information called 'Addenda', is deleted by default to minimize the exponential creation of new tables.

*Mundo Inmobiliario S.A*
