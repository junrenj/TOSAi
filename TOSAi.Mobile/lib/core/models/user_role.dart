enum UserRole {
  student,
  parent;

  String get apiName => switch (this) {
        UserRole.student => 'Student',
        UserRole.parent => 'Parent',
      };

  String get label => switch (this) {
        UserRole.student => '学生端',
        UserRole.parent => '家长端',
      };
}
