using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration;
using System.Linq;
using Devart.Data.Oracle;
using NUnit.Framework;

namespace Temp.Tests
{
  [TestFixture]
  public class DevartTests
  {
    private const string mConnectionString = "Host=localhost;Port=1521;User ID=SCD;Password=test;Service Name=ORCL19PDB1;Direct=true";

    [Test]
    public void TestWithOldVersionOfDevart()
    {
      new OracleMonitor { IsActive = true };

      var d1 = CreateNewDirector();
      var d2 = CreateNewDirector();

      int cityId;
      using (var context = new Context())
      {
        City city = new City()
        {
          Name = "Alba Iulia",
          Schools = new List<School>
          {
            new School
            {
              Name="HCC",
            Director=context.Set<Director>().AsQueryable<Director>().Single(x => x.Id == d1.Id)
      }
        }
        };

        context.Set<City>().Add(city);

        TrackChanges(context);

        context.SaveChanges();

        cityId = city.Id;
      }

      Console.WriteLine("==========Update=================");

      using (var context2 = new Context())
      {
        var city = context2.Set<City>().AsQueryable<City>()
          .Include(x => x.Schools)
          .Include("Schools.Parent")
          .Include("Schools.Director")
          .First(x => x.Id == cityId);

        city.Schools[0].Director = context2.Set<Director>().AsQueryable<Director>().Single(x => x.Id == d2.Id);
        //city.Schools[0].Name = Guid.NewGuid().ToString();

        TrackChanges(context2);
        int numberOfChanges = context2.SaveChanges();

        Console.WriteLine(numberOfChanges);
      }
    }

    private static Director CreateNewDirector()
    {
      var context = new Context();

      var directories = context.Set<Director>();

      Director director = new Director()
      {
        Name = Guid.NewGuid().ToString()
      };

      directories.Add(director);

      context.SaveChanges();

      return director;
    }

    private static void TrackChanges(Context context)
    {
      context.ChangeTracker.DetectChanges();
      var entries = context.ChangeTracker.Entries().ToArray();

      foreach (var entry in entries)
      {
        Console.WriteLine($"{entry.Entity.GetType().Name} {entry.State}");
      }
    }

    public class Context : DbContext
    {
      public Context()
      : base(new OracleConnection(mConnectionString), false)
      {
        Database.SetInitializer<Context>(null);
        Configuration.AutoDetectChangesEnabled = true;
      }

      protected override void OnModelCreating(DbModelBuilder modelBuilder)
      {
        modelBuilder.Configurations.Add(new CityConfiguration());
        modelBuilder.Configurations.Add(new SchoolConfiguration());
        modelBuilder.Configurations.Add(new DirectorConfiguration());
      }
    }

    public class City
    {
      public City()
      {
        Schools = new List<School>();
      }

      [Key]
      public int Id { get; set; }

      public string Name { get; set; }

      public IList<School> Schools { get; set; }
    }

    public class School
    {
      [Key]
      public int Id { get; set; }

      public string Name { get; set; }

      public City Parent { get; set; }

      public Director Director { get; set; }
    }

    public class Director
    {
      [Key]
      public int Id { get; set; }

      public string Name { get; set; }
    }

    public class SchoolConfiguration : EntityTypeConfiguration<School>
    {
      public SchoolConfiguration()
      {
        HasKey(x => x.Id);

        Property(x => x.Id).HasColumnName("ID").HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        Property(x => x.Name).HasColumnName("NAME");

        HasRequired(x => x.Parent).WithMany(x => x.Schools).Map(c => c.MapKey("FKCITYID")).WillCascadeOnDelete(true);
        //HasOptional(x => x.Director).WithMany().Map(c => c.MapKey("FKDIRECTORID")).WillCascadeOnDelete(false);

        HasOptional(x => x.Director).WithOptionalDependent().Map(c => c.MapKey("FKDIRECTORID")).WillCascadeOnDelete(false);

        ToTable("SCHOOL");
      }
    }

    public class CityConfiguration : EntityTypeConfiguration<City>
    {
      public CityConfiguration()
      {
        HasKey(x => x.Id);

        Property(x => x.Id).HasColumnName("ID").HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        Property(X => X.Name).HasColumnName("NAME");

        ToTable("CITY");
      }
    }

    public class DirectorConfiguration : EntityTypeConfiguration<Director>
    {
      public DirectorConfiguration()
      {
        HasKey(x => x.Id);

        Property(x => x.Id).HasColumnName("ID").HasDatabaseGeneratedOption(DatabaseGeneratedOption.Identity);
        Property(X => X.Name).HasColumnName("NAME");

        ToTable("DIRECTOR");
      }
    }
  }
}