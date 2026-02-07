-- useful results query

  select w.WeekNumber, s.SeasonNumber, t1.Name, t2.Name, p1.UserName, p2.UserName from matches m
  inner join dbo.Weeks w on w.Id = m.WeekId
  inner join dbo.Seasons s on s.Id = w.SeasonId
  inner join dbo.Players p1 on m.Player1Id = p1.Id
  inner join dbo.Players p2 on m.Player2Id = p2.Id
  inner join dbo.PlayerSeasonTeams pst1 on pst1.PlayerId = p1.Id and pst1.SeasonId = s.Id
  inner join dbo.PlayerSeasonTeams pst2 on pst2.PlayerId = p2.Id and pst2.SeasonId = s.Id
  inner join dbo.Teams t1 on t1.Id = pst1.TeamId
  inner join dbo.Teams t2 on t2.Id = pst2.TeamId
  order by WeekId