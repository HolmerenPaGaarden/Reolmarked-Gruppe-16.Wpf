Reolmarked – Gruppe 16

Udvikler: Peter Andreas Udengaard
Dato: 2025-10-06
Repository: https://github.com/HolmerenPaGaarden/Reolmarked-Gruppe-16.Wpf


Formål

Reolmarked er et digitalt administrationssystem til et fysisk kommissionsmarked.
Programmet håndterer:
Lejere, reoler, produkter og salg
Afregning og betaling
Eksport af rapporter (PDF og CSV)
Målet er at digitalisere manuelle processer og give et samlet overblik over reoler, varer og økonomi i realtid.


Systemarkitektur

Arkitekturprincipper (Tulipanmodellen)
UI (WPF): Brugerflade med faner for Opret vare, Salg, Produkter, Lejere & Reoler, Afregning & Betaling
Services (Forretningslogik):
ProductService, SaleService, LeaseService, AccountingService, PaymentService, BarcodeService
Data (EF Core): ReolContext (DbContext)
Database: SQLite (reolmarked.db)
Eksterne biblioteker:
QRCoder
 – generering af QR-koder
QuestPDF
 – generering af PDF-rapporter


Principper

UI kalder kun Services (ingen direkte SQL).
Services bruger Entity Framework Core til databaseoperationer.
Eksterne biblioteker anvendes via små wrapper-services for løs kobling.


Systemflow

Opret lejer med kontaktoplysninger
Opret reol og tilknyt reolen til en lejer via lejeaftale (pris, kommission, periode)
Opret produkt og generér automatisk QR-label
Scan QR-kode ved salg → registrér automatisk i databasen
Afregning: Systemet beregner omsætning, kommission, reolleje og netto
Eksportér afregning til PDF eller CSV
Registrér betaling (butik ↔ lejer)

Datamodellering og normalisering

1NF – Første normalform
Én tabel med alle oplysninger (Lejer, Reol, Produkt, Salg).
Alle værdier er atomare, men der forekommer redundans og afhængigheder.

2NF – Anden normalform
Delvise afhængigheder fjernet: separate tabeller for Lejer, Reol, Produkt, Salg.
Dog stadig transitive afhængigheder (Produkt → Lejer via Reol).

3NF – Tredje normalform
Introduktion af Lejeaftale med vilkår (pris, kommission, periode).
Ingen delvise eller transitive afhængigheder.
Endelig model inkluderer:
Lejer, Reol, Lejeaftale, Produkt, Salg, Afregning, Afregningslinje, Betaling.


Use Cases

ID	Use Case	Beskrivelse	Implementeret
UC1	Opret lejer	Administrator kan oprette lejere med kontaktinfo	✅
UC2	Opret reol	Administrator kan registrere reoler	✅
UC3	Tildel reol	Lejer tildeles en eller flere reoler for en periode	✅
UC4	Opret produkt	Vare registreres på reol og får QR-label	✅
UC5	Sælg produkt	Produkt sælges ved scanning af QR-kode	✅
UC6	Generér afregning	Beregner omsætning, kommission, reolleje, netto	✅
UC7	Registrér betaling	Gemmer betalinger i databasen	✅
UC8	Eksportér rapport	Eksporterer PDF/CSV via QuestPDF	✅
UC9	Se oversigter	Viser lejere, reoler, produkter og aftaler	✅

Programmering

Projektstruktur
Reolmarked.Wpf/
│
├── App.xaml
├── MainWindow.xaml / .cs
│
├── Data/
│   ├── Models.cs
│   └── ReolContext.cs
│
├── Services/
│   ├── ProductService.cs
│   ├── SaleService.cs
│   ├── LeaseService.cs
│   ├── AccountingService.cs
│   ├── PaymentService.cs
│   └── BarcodeService.cs
│
└── labels/ & exports/  (QR-labels og PDF/CSV-filer)


Anvendte teknologier

.NET 9.0 / C#
WPF
Entity Framework Core (SQLite)
QRCoder
QuestPDF


Udviklingsproces

Oprettelse af models, DbContext og initial test i Console.
Tilføjelse af service-klasser: Product, Sale, Barcode.
Udbygning med Lease, Accounting og Payment services.
Implementering af WPF-frontend med tre første faner.
Tilføjelse af “Lejere & Reoler” + “Afregning & Betaling”.
Tilføjelse af PDF/CSV-eksport.
Test og oprydning.

Resultatet er en fuldt funktionel WPF-applikation med database, QR-label-system og automatiseret afregning.


Kvalitetskriterier

Artefakt	Kriterium
Domænemodel	Korrekt repræsentation af virkelige begreber og relationer
DCD	Klasser og metoder følger ansvarsprincipper
Tulipanmodel	Klar adskillelse mellem præsentation, logik og data
Normalisering	Ingen redundans, alle tabeller opfylder 3NF
Flowchart	Viser sammenhængen fra oprettelse → salg → afregning


Fremtidige forbedringer

Implementere MVVM for bedre testbarhed
Tilføje Dependency Injection og Unit Tests
Udvide med brugerlogin og rollehåndtering
Cloud-synkronisering eller integration med regnskabssystem



Konklusion

Systemet opfylder alle opstillede use cases.
Koden er struktureret, logisk og udbygget med moderne biblioteker.
Projektet demonstrerer en fuldt funktionel løsning, der kan skaleres og udvides med flere funktioner.
