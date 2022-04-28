using Application.Data;
using Application.Entities;
using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Application.Commands
{
    public class GetPaymentCommand : IRequest<GetPaymentCommandResult>
    {
        public string Id { get; set; }
        public string Reference { get; set; }
    }

    public class GetPaymentCommandHandler : IRequestHandler<GetPaymentCommand, GetPaymentCommandResult>
    {
        private readonly IAsyncRepository<Payment> _paymentRepository;

        private Payment _payment;
        private GetPaymentCommandResult _result;

        public GetPaymentCommandHandler(
            IAsyncRepository<Payment> paymentRepository)
        {
            _paymentRepository = paymentRepository;
        }

        public async Task<GetPaymentCommandResult> Handle(GetPaymentCommand request, CancellationToken cancellationToken)
        {
            await GetPayment(request);

            BuildResult();

            return _result;
        }

        private async Task GetPayment(GetPaymentCommand request)
        {
            var id = request.Id == null
                ? new Guid()
                : new Guid(request.Id);

            _payment = (await _paymentRepository.Get(x => (request.Id != null && x.Identifier == id) || (request.Reference != null || x.Reference == request.Reference))).Data;
        }

        private void BuildResult()
        {
            _result = new GetPaymentCommandResult()
            {
                Payment = _payment
            };
        }
    }
}
