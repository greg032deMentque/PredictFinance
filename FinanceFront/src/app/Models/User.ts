
import { UserRole } from './user-role';


export class User {
  Id = "";
  UserName = "";
  FirstName = "";
  LastName = "";
  FullName = "";
  Email = "";
  Created = new Date();
  LastActive = new Date();
  Password= "";
  PasswordPlanio = "";
  DailyRate = 0;
  Updated = new Date();
  Projects = [];
  Roles : UserRole[] = [];
}
