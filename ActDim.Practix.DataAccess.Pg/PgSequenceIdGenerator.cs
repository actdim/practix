
using Conditions;
using Orthobits.Abstractions.DataAccess;
using OrthoBits.Abstractions.DataAccess;
using System.Data.Common;

namespace Orthobits.DataAccess
{
    internal class PgSequenceIdGenerator : ISequenceIdGenerator
    {   
        public PgSequenceIdGenerator()
        {
 
        }     

        public long GetNewId(DbConnection connection, string sequenceName)
        {            
            using var cmd = connection.CreateCommand();            
            cmd.CommandText = $"SELECT NEXTVAL('{sequenceName}')";
            var result = (long)cmd.ExecuteScalar();
            return result;
        }

        // TODO:
        // https://www.cybertec-postgresql.com/en/sequences-gains-and-pitfalls/
        // CREATE OR REPLACE FUNCTION multi_nextval(
        //    use_seqname regclass,
        //    use_increment integer
        // ) RETURNS bigint AS $$
        // DECLARE
        //    reply bigint;
        //    lock_id bigint := use_seqname::bigint;
        // BEGIN
        //    PERFORM pg_advisory_lock(lock_id);
        //    reply := nextval(use_seqname);
        //    PERFORM setval(use_seqname, reply + use_increment - 1, TRUE);
        //    PERFORM pg_advisory_unlock(lock_id);
        //    RETURN reply + increment - 1;
        // END;
        // $$ LANGUAGE plpgsql;
        
    }
}