# MarketStock Manager

![Build](https://github.com/KORISNIK/marketstock-manager/actions/workflows/build.yml/badge.svg)

**MarketStock Manager** je desktop aplikacija za vođenje male prodavnice ili supermarketa. Projekat je napravljen kao portfolio aplikacija za C# programera: ima lokalnu bazu, CRUD operacije, prodaju, evidenciju zaliha, dashboard i izveštaje.

> Ovo nije web frontend i nije API projekat. Ovo je Windows desktop aplikacija napravljena u C# WPF tehnologiji.

## Tehnologije

- C#
- .NET 8
- WPF
- SQLite
- Microsoft.Data.Sqlite
- Visual Studio

## Glavne funkcionalnosti

- Dashboard sa ključnim metrikama
- Upravljanje proizvodima
- Kategorije proizvoda
- Pretraga proizvoda po nazivu, SKU i barkodu
- Filter po kategoriji
- Pregled proizvoda sa niskim zalihama
- Evidencija ulaza robe
- Evidencija otpisa robe
- Korekcija zaliha
- Prodaja proizvoda kroz račun
- Automatsko umanjenje zaliha nakon prodaje
- Istorija promena zaliha
- Pregled poslednjih računa
- Izveštaj najprodavanijih proizvoda
- Lokalna SQLite baza koja se automatski kreira
- Demo podaci pri prvom pokretanju

## Zašto je dobar za portfolio

Ovaj projekat pokazuje da znaš da napraviš realnu poslovnu aplikaciju, a ne samo običan CRUD primer. U aplikaciji postoje realni tokovi rada:

- proizvodi imaju SKU, barkod, cenu i minimalnu zalihu
- prodaja proverava da li ima dovoljno robe na stanju
- svaka promena zaliha se čuva u istoriji (uključujući ručnu izmenu stanja kroz formu proizvoda)
- dashboard odmah pokazuje stanje poslovanja
- baza se kreira automatski i aplikacija može da se koristi odmah

## Struktura projekta

```text
MarketStockManager/
├── MarketStockManager.sln
├── README.md
├── LICENSE
├── .gitignore
├── .gitattributes
├── .github/
│   └── workflows/
│       └── build.yml
├── docs/
│   └── database-schema.sql
└── src/
    └── MarketStockManager/
        ├── MarketStockManager.csproj
        ├── App.xaml
        ├── App.xaml.cs
        ├── MainWindow.xaml
        ├── MainWindow.xaml.cs
        ├── Data/
        │   └── DatabaseService.cs
        └── Models/
            ├── Category.cs
            ├── Product.cs
            ├── StockMovement.cs
            ├── Sale.cs
            ├── SaleItem.cs
            ├── SaleCartItem.cs
            ├── DashboardStats.cs
            └── ReportRow.cs
```

## Pokretanje projekta

### Opcija 1: Visual Studio

1. Otvori `MarketStockManager.sln`
2. Sačekaj da Visual Studio uradi restore NuGet paketa
3. Pokreni projekat na `Start` ili `F5`

### Opcija 2: Terminal

```bash
dotnet restore
dotnet run --project src/MarketStockManager
```

Napomena: pošto je WPF Windows tehnologija, projekat se pokreće na Windows računaru.

## Baza podataka

Aplikacija automatski kreira SQLite bazu `marketstock.db` pri prvom pokretanju. Baza se nalazi u output folderu aplikacije, na primer:

```text
src/MarketStockManager/bin/Debug/net8.0-windows/marketstock.db
```

Ako želiš da resetuješ demo podatke, ugasi aplikaciju i obriši `marketstock.db`. Pri sledećem pokretanju aplikacija će ponovo napraviti bazu i ubaciti demo podatke.

## Demo podaci

Pri prvom pokretanju automatski se kreiraju kategorije:

- Pića
- Mlečni proizvodi
- Pekara
- Slatkiši
- Hemija

Dodati su i primeri proizvoda kao što su mineralna voda, mleko, hleb, čokolada i deterdžent.

## Licenca

Projekat je objavljen pod [MIT licencom](LICENSE).
