using System;using System.Collections.Generic;using System.Linq;
namespace XzyBattleStats {
sealed class RoleScoreSummary {public long? Delta;public int Recorded,RankedGames;}
static class RoleScoreStats {
 public static RoleScoreSummary For(IEnumerable<Battle> battles){var ranked=battles.Where(b=>b.DetailStatus=="ok"&&(b.Mode==12||b.Mode==22)).ToList();var actual=ranked.Where(b=>b.ScoreBefore.HasValue&&b.ScoreAfter.HasValue).ToList();return new RoleScoreSummary{Delta=actual.Count==0?(long?)null:actual.Sum(b=>b.ScoreAfter.Value-b.ScoreBefore.Value),Recorded=actual.Count,RankedGames=ranked.Count};}
}
}
