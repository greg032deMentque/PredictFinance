export class PaginateInterface<T> {
  constructor(
    public Items: T[] = [],
    public Total = 0,
    public Page = 0,
    public PageSize = 0

  ) { }
}


