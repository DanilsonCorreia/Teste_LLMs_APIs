using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Tensorflow.Util;
using TesteLLMs.Models;



namespace TesteLLMs.Services
{
    public class DBContext : DbContext
    {
        public DbSet<Embeddings> Embeddings { get; set; }

        public DBContext(DbContextOptions<DBContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Embeddings>()
                .HasKey(e => e.Id);

            modelBuilder.Entity<Embeddings>()
                .Property(e => e.Id)
                .ValueGeneratedOnAdd();

            modelBuilder.Entity<Embeddings>()
                .Property(e => e.Embedding)
                .HasColumnType("vector(1536)");


        }
    }

    public class FloatArrayToBytesConverter : ValueConverter<float[], byte[]>
    {
        public FloatArrayToBytesConverter()
            : base(
                arr => FloatArrayToBytes(arr),
                bytes => BytesToFloatArray(bytes))
        {
        }

        private static byte[] FloatArrayToBytes(float[] arr)
        {
            if (arr == null) return null;
            var bytes = new byte[arr.Length * sizeof(float)];
            Buffer.BlockCopy(arr, 0, bytes, 0, bytes.Length);
            return bytes;
        }

        private static float[] BytesToFloatArray(byte[] bytes)
        {
            if (bytes == null) return null;
            var floats = new float[bytes.Length / sizeof(float)];
            Buffer.BlockCopy(bytes, 0, floats, 0, bytes.Length);
            return floats;
        }
    }


}
