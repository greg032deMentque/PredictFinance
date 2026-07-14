import { UserRole } from './user-role';

export class User {
  Id = '';
  UserName = '';
  FirstName = '';
  LastName = '';
  FullName = '';
  Email = '';
  PhoneNumber = '';
  IsActive = true;
  Created = new Date();
  CreatedAt?: Date | string;
  LastActive = new Date();
  LastConnection?: Date | string;
  Password = '';
  PasswordPlanio = '';
  DailyRate = 0;
  Updated = new Date();
  Projects = [];
  Roles: UserRole[] = [];
}
