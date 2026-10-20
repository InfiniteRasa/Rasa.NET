using System;
using System.Collections.Generic;
using System.Linq;

namespace Rasa.Repositories.Char.Petition
{
    using Context.Char;
    using Structures.Char;

    public class PetitionRepository : IPetitionRepository
    {
        private readonly CharContext _charContext;

        public PetitionRepository(CharContext charContext)
        {
            _charContext = charContext;
        }

        /// <summary>
        /// Reports the generated id rather than throwing. This runs inside a packet handler, so
        /// an exception here would cost the player their connection over a failed insert - and
        /// the client already has a "petition failed" message for exactly this case.
        /// </summary>
        public uint AddPetition(PetitionEntry entry)
        {
            try
            {
                _charContext.PetitionEntries.Add(entry);
                _charContext.SaveChanges();

                return entry.Id;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error adding Petition:");
                Logger.WriteLog(LogType.Error, e);
                return 0;
            }
        }

        public PetitionEntry GetPetition(uint id)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.PetitionEntries);

            return query.FirstOrDefault(e => e.Id == id);
        }

        public List<PetitionEntry> ListPetitions(byte? status, int limit)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.PetitionEntries);

            if (status.HasValue)
                query = query.Where(e => e.Status == status.Value);

            return query.OrderByDescending(e => e.Id).Take(limit).ToList();
        }

        public List<PetitionEntry> ListPetitionsForAccount(uint accountId, int limit)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.PetitionEntries);

            return query.Where(e => e.AccountId == accountId)
                        .OrderByDescending(e => e.Id)
                        .Take(limit)
                        .ToList();
        }

        public bool UpdatePetitionBody(uint id, string body)
        {
            try
            {
                var entry = _charContext.GetWritable(_charContext.PetitionEntries, id);

                if (entry == null)
                    return false;

                entry.Body = body;

                _charContext.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error appending to Petition:");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }
        }

        /// <summary>
        /// Reports whether the row was found, the same way AddPetition reports its id: the
        /// cancel path runs inside a packet handler, where a throw costs the connection.
        /// </summary>
        public bool SetPetitionStatus(uint id, byte status, string resolution)
        {
            try
            {
                var entry = _charContext.GetWritable(_charContext.PetitionEntries, id);

                if (entry == null)
                    return false;

                entry.Status = status;
                entry.Resolution = resolution ?? string.Empty;

                _charContext.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error updating Petition:");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }
        }

        public bool SetPetitionResolution(uint id, string resolution)
        {
            try
            {
                var entry = _charContext.GetWritable(_charContext.PetitionEntries, id);

                if (entry == null)
                    return false;

                entry.Resolution = resolution ?? string.Empty;

                _charContext.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error answering Petition:");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }
        }

        public bool SetNoticePending(uint id, bool pending)
        {
            try
            {
                var entry = _charContext.GetWritable(_charContext.PetitionEntries, id);

                if (entry == null)
                    return false;

                entry.NoticePending = pending;

                _charContext.SaveChanges();
                return true;
            }
            catch (Exception e)
            {
                Logger.WriteLog(LogType.Error, "Error updating Petition notice:");
                Logger.WriteLog(LogType.Error, e);
                return false;
            }
        }

        public List<PetitionEntry> ListNoticesPending(uint accountId)
        {
            var query = _charContext.CreateNoTrackingQuery(_charContext.PetitionEntries);

            return query.Where(e => e.AccountId == accountId && e.NoticePending)
                        .OrderBy(e => e.Id)
                        .ToList();
        }
    }
}
