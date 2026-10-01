            row["DoctorName"]=e.Doctor?.User!=null ? $"{e.Doctor.User.FirstName} {e.Doctor.User.LastName}" : "";
      
            row["Date"]=(e.ScheduledStart??e.EncounterDate).ToString("dd.MM.yyyy HH:mm");
            row["Status"]=new SparkBadgeValue(
                EncounterStatusSchema.ToDisplay(e.Status.ToString()),
                StatusToTone(e.Status));

            // Select goes to Detail (view), Edit goes to a different route (Edit) —
            // can't use AddDefaultActions here since both actions use base commands
            // that point at the same DetailRoute; these navigate to different routes.
            var actions = new List<SparkButtonItem>
            {
                new SparkButtonItem
                {
                    IsPrimary=true,
                    IconGlyph="👁",
                    Label="Детали",
                    Command=SelectCommand,
                    CommandParameter=e
                }
            };

            if(CanUpdate)
                actions.Add(new SparkButtonItem { IconGlyph="✎", Label="Промени", Command=EditCommand, CommandParameter=e });

            row["Actions"]=actions;
            rows.Add(row);
        }
        GridRows=rows;
    }
