﻿using System;
using System.Data.SqlClient;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using PeanutButter.RandomGenerators;
using PeanutButter.TempDb.LocalDb;
using PeanutButter.Utils;
using PeanutButter.TestUtils.Generic;
// ReSharper disable InconsistentNaming

namespace PeanutButter.TempDb.Tests
{
    [TestFixture]
    public class TestTempDBLocalDb
    {
        [Test]
        public void ShouldImplementIDisposable()
        {
            //---------------Set up test pack-------------------

            //---------------Assert Precondition----------------

            //---------------Execute Test ----------------------
            typeof(TempDBLocalDb).ShouldImplement<IDisposable>();

            //---------------Test Result -----------------------
        }

        [Test]
        public void Construct_ShouldCreateTemporaryLocalDbDatabase()
        {
            //---------------Set up test pack-------------------
            using (var db = new TempDBLocalDb())
            {
                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------

                //---------------Test Result -----------------------
                Assert.IsTrue(File.Exists(db.DatabasePath));
                using (var conn = new SqlConnection(db.ConnectionString))
                {
                    Assert.DoesNotThrow(conn.Open);
                }
            }
        }

        [Test]
        public void Construct_ShouldBeAbleToCreateDbByName()
        {
            //---------------Set up test pack-------------------
            var dbName = RandomValueGen.GetRandomAlphaString(5, 10);
            using (var db = new TempDBLocalDb(dbName, null))
            {
                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------

                //---------------Test Result -----------------------
                Assert.IsTrue(File.Exists(db.DatabasePath));
                using (var conn = new SqlConnection(db.ConnectionString))
                {
                    Assert.DoesNotThrow(conn.Open);
                }
            }
        }

        [Test]
        public void Dispose_ShouldRemoveTheTempDatabase()
        {
            //---------------Set up test pack-------------------
            string file;
            using (var db = new TempDBLocalDb())
            {
                //---------------Assert Precondition----------------
                var conn = db.CreateConnection();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "create table [test] ([id] int primary key identity, [name] nvarchar(128));";
                cmd.ExecuteNonQuery();
                cmd = conn.CreateCommand();
                cmd.CommandText = "insert into [test] ([name]) values ('the name');";
                cmd.ExecuteNonQuery();
                cmd = conn.CreateCommand();
                cmd.CommandText = "select * from [test];";
                cmd.ExecuteReader();
                file = db.DatabasePath;
                Assert.IsTrue(File.Exists(file));

                //---------------Execute Test ----------------------

                //---------------Test Result -----------------------
            }
            Assert.IsFalse(File.Exists(file));
        }

        [Test]
        public void Construct_ShouldRunGivenScriptsOnDatabase()
        {
            var createTable = "create table TheTable(id int primary key, name nvarchar(128));";
            var insertData = "insert into TheTable(id, name) values (1, 'one');";
            var selectData = "select name from TheTable where id = 1;";
            using (var db = new TempDBLocalDb(new[] { createTable, insertData }))
            {
                //---------------Set up test pack-------------------

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                using (var conn = new SqlConnection(db.ConnectionString))
                {
                    conn.Open();
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = selectData;
                        using (var rdr = cmd.ExecuteReader())
                        {
                            Assert.IsTrue(rdr.Read());
                            Assert.AreEqual("one", rdr["name"].ToString());
                        }
                    }
                }

                //---------------Test Result -----------------------
            }
        }

        [Test]
        public void GetConnection_ShouldReturnValidConnection()
        {
            var createTable = "create table TheTable(id int primary key, name nvarchar(128));";
            var insertData = "insert into TheTable(id, name) values (1, 'one');";
            var selectData = "select name from TheTable where id = 1;";
            using (var db = new TempDBLocalDb(new[] { createTable, insertData }))
            {
                //---------------Set up test pack-------------------

                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                using (var conn = db.CreateConnection())
                {
                    using (var cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = selectData;
                        using (var rdr = cmd.ExecuteReader())
                        {
                            Assert.IsTrue(rdr.Read());
                            Assert.AreEqual("one", rdr["name"].ToString());
                        }
                    }
                }

                //---------------Test Result -----------------------
            }
        }

        [Test]
        public void Dispose_ShouldCloseManagedConnectionsBeforeAttemptingToDeleteTheFile()
        {
            var createTable = "create table TheTable(id int primary key, name nvarchar(128));";
            var insertData = "insert into TheTable(id, name) values (1, 'one');";
            var selectData = "select * from TheTable; "; //"select name from TheTable where id = 1;";
            string theFile;
            using (var db = new TempDBLocalDb(new[] { createTable, insertData }))
            {
                theFile = db.DatabasePath;
                Assert.IsTrue(File.Exists(theFile));
                //---------------Set up test pack-------------------

                //---------------Assert Precondition----------------

                var conn = db.CreateConnection();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = selectData;
                    using (var rdr = cmd.ExecuteReader())
                    {
                        Assert.IsTrue(rdr.Read());
                        Assert.AreEqual("one", rdr["name"].ToString());
                    }
                }
                //---------------Execute Test ----------------------
            }
            //---------------Test Result -----------------------
            Assert.IsFalse(File.Exists(theFile));
        }

        [Test]
        public void ShouldPlayNicelyInParallel()
        {
            //---------------Set up test pack-------------------
            using (var disposer = new AutoDisposer())
            {
                //---------------Assert Precondition----------------

                //---------------Execute Test ----------------------
                // localDb seems to take a little longer to spin up; reduced the parallel count to 10 to make
                //  running all tests viable
                Parallel.For(0, 10, i =>
                {
                    // ReSharper disable once AccessToDisposedClosure
                    disposer.Add(new TempDBLocalDb());
                });

                //---------------Test Result -----------------------
            }
        }
    }
}